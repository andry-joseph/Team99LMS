using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo("LMSControllerTests")]
namespace LMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private LMSContext db;
        public StudentController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Catalog()
        {
            return View();
        }

        public IActionResult Class(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult Assignment(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }


        public IActionResult ClassListings(string subject, string num)
        {
            System.Diagnostics.Debug.WriteLine(subject + num);
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }


        /*******Begin code to modify********/

        /// <summary>
        /// Returns a JSON array of the classes the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester
        /// "year" - The year part of the semester
        /// "grade" - The grade earned in the class, or "--" if one hasn't been assigned
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {
            var query = from c in db.Classes
                        join cr in db.Courses on c.CrId equals cr.CrId into ccr
                        from x in ccr.DefaultIfEmpty()
                        join e in db.Enrollments on c.CId equals e.CId into xe
                        from enroll in xe.DefaultIfEmpty()
                        where enroll.Student == uid
                        select new { subject = x.Department, number = x.CNum, name = x.CName, season = c.Semester, year = c.Year, grade = enroll.Grade == null ? "--" : enroll.Grade };

            return Json(query.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all the assignments in the given class that the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The category name that the assignment belongs to
        /// "due" - The due Date/Time
        /// "score" - The score earned by the student, or null if the student has not submitted to this assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="uid"></param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInClass(string subject, int num, string season, int year, string uid)
        {
            var query = from c in db.Classes
                        join course in db.Courses on c.CrId equals course.CrId
                        join enroll in db.Enrollments on c.CId equals enroll.CId
                        join assignCat in db.AssignmentCategories on c.CId equals assignCat.CId
                        join assignment in db.Assignments on assignCat.AcId equals assignment.AcId
                        where course.Department == subject
                        && course.CNum == num
                        && c.Semester == season
                        && c.Year == year
                        && enroll.Student == uid
                        select new
                        {
                            aname = assignment.AName,
                            cname = assignCat.CatName,
                            due = assignment.DueDate,
                            score = db.Submissions
                                .Where(s => s.Student == uid && s.AId == assignment.AId)
                                .Select(s => s.Score)
                                .FirstOrDefault()
                        };

            return Json(query.ToArray());
        }



        /// <summary>
        /// Adds a submission to the given assignment for the given student
        /// The submission should use the current time as its DateTime
        /// You can get the current time with DateTime.Now
        /// The score of the submission should start as 0 until a Professor grades it
        /// If a Student submits to an assignment again, it should replace the submission contents
        /// and the submission time (the score should remain the same).
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="uid">The student submitting the assignment</param>
        /// <param name="contents">The text contents of the student's submission</param>
        /// <returns>A JSON object containing {success = true/false}</returns>
        public IActionResult SubmitAssignmentText(string subject, int num, string season, int year,
          string category, string asgname, string uid, string contents)
        {
            var query = from c in db.Classes
                        join cr in db.Courses on c.CrId equals cr.CrId into ccr
                        from x in ccr.DefaultIfEmpty()
                        join ac in db.AssignmentCategories on c.CId equals ac.CId into xac
                        from assignCat in xac.DefaultIfEmpty()
                        join a in db.Assignments on assignCat.AcId equals a.AcId into assigna
                        from assignment in assigna.DefaultIfEmpty()
                        where x.Department == subject
                        && x.CNum == num
                        && c.Semester == season
                        && c.Year == year
                        && assignment.AName == asgname
                        && assignCat.CatName == category
                        select new { aID = assignment.AId };

            uint aID = 0;
            foreach (var v in query)
            {
                aID = v.aID;
            }

            if (aID == 0)
            {
                return Json(new { success = false });
            }

            try
            {
                var existingSubmission = db.Submissions
                    .FirstOrDefault(s => s.AId == aID && s.Student == uid);

                if (existingSubmission != null)
                {
                    existingSubmission.Time = DateTime.Now;
                    existingSubmission.StudentSolution = contents;
                }
                else
                {
                    Submission newSub = new Submission();
                    newSub.AId = aID;
                    newSub.Student = uid;
                    newSub.Time = DateTime.Now;
                    newSub.StudentSolution = contents;
                    db.Submissions.Add(newSub);
                }

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }


        /// <summary>
        /// Enrolls a student in a class.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing {success = {true/false}. 
        /// false if the student is already enrolled in the class, true otherwise.</returns>
        public IActionResult Enroll(string subject, int num, string season, int year, string uid)
        {
            var query = from c in db.Classes
                        join cr in db.Courses on c.CrId equals cr.CrId into ccr
                        from x in ccr.DefaultIfEmpty()
                        where x.Department == subject
                        && x.CNum == num
                        && c.Semester == season
                        && c.Year == year
                        select new { cID = c.CId };

            uint cID = 0;
            foreach (var v in query)
            {
                cID = v.cID;
            }
            try
            {
                Enrollment enroll = new Enrollment();
                enroll.Student = uid;
                enroll.CId = cID;
                db.Enrollments.Add(enroll);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }



        /// <summary>
        /// Calculates a student's GPA
        /// A student's GPA is determined by the grade-point representation of the average grade in all their classes.
        /// Assume all classes are 4 credit hours.
        /// If a student does not have a grade in a class ("--"), that class is not counted in the average.
        /// If a student is not enrolled in any classes, they have a GPA of 0.0.
        /// Otherwise, the point-value of a letter grade is determined by the table on this page:
        /// https://advising.utah.edu/academic-standards/gpa-calculator-new.php
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing a single field called "gpa" with the number value</returns>
        public IActionResult GetGPA(string uid)
        {
            var query = from e in db.Enrollments
                        where e.Student == uid
                        select new { grade = e.Grade };

            var totalHours = 0;
            double totalGradePoints = 0.0;

            if (query.ToArray().Length == 0)
            {
                return Json(new { gpa = 0.0 });
            }

            foreach (var v in query)
            {
                if (v.grade != null && v.grade != "--")
                {
                    totalHours += 4;

                    switch (v.grade)
                    {
                        case "A":
                            totalGradePoints += 4.0;
                            break;
                        case "A-":
                            totalGradePoints += 3.7;
                            break;
                        case "B+":
                            totalGradePoints += 3.3;
                            break;
                        case "B":
                            totalGradePoints += 3.0;
                            break;
                        case "B-":
                            totalGradePoints += 2.7;
                            break;
                        case "C+":
                            totalGradePoints += 2.3;
                            break;
                        case "C":
                            totalGradePoints += 2.0;
                            break;
                        case "C-":
                            totalGradePoints += 1.7;
                            break;
                        case "D+":
                            totalGradePoints += 1.3;
                            break;
                        case "D":
                            totalGradePoints += 1.0;
                            break;
                        case "D-":
                            totalGradePoints += 0.7;
                            break;
                        case "E":
                            totalGradePoints += 0.0;
                            break;
                    }
                }
            }

            if (totalHours == 0)
            {
                return Json(new { gpa = 0.0 });
            }

            totalGradePoints *= 4;
            return Json(new { gpa = totalGradePoints / totalHours });
        }

        /*******End code to modify********/

    }
}
