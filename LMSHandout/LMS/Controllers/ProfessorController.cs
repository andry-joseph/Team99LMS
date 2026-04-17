using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS_CustomIdentity.Controllers
{
    [Authorize(Roles = "Professor")]
    public class ProfessorController : Controller
    {

        private readonly LMSContext db;

        public ProfessorController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Students(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
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

        public IActionResult Categories(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult CatAssignments(string subject, string num, string season, string year, string cat)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
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

        public IActionResult Submissions(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }

        public IActionResult Grade(string subject, string num, string season, string year, string cat, string aname, string uid)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            ViewData["uid"] = uid;
            return View();
        }

        /*******Begin code to modify********/


        /// <summary>
        /// Returns a JSON array of all the students in a class.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "dob" - date of birth
        /// "grade" - the student's grade in this class
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetStudentsInClass(string subject, int num, string season, int year)
        {

            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join e in db.Enrollments on c.CId equals e.CId into ec
                            from enroll in ec.DefaultIfEmpty()
                            join s in db.Students on enroll.Student equals s.UId into senroll
                            from student in senroll.DefaultIfEmpty()
                            where x.Department == subject && x.CNum == num && c.Semester == season && c.Year == year
                            select new {fname = student.FirstName, lname = student.LastName, uid = student.UId, dob = student.Dob, grade = enroll.Grade};


            return Json(query.ToArray());
        }



        /// <summary>
        /// Returns a JSON array with all the assignments in an assignment category for a class.
        /// If the "category" parameter is null, return all assignments in the class.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The assignment category name.
        /// "due" - The due DateTime
        /// "submissions" - The number of submissions to the assignment
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class, 
        /// or null to return assignments from all categories</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInCategory(string subject, int num, string season, int year, string category)
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
                            && (category == null || assignCat.CatName == category)
                            select new {aname = assignment.AName, cname = assignCat.CatName, due = assignment.DueDate, submissions = assignment.Submissions};

            return Json(query.ToArray());
        }


        /// <summary>
        /// Returns a JSON array of the assignment categories for a certain class.
        /// Each object in the array should have the folling fields:
        /// "name" - The category name
        /// "weight" - The category weight
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentCategories(string subject, int num, string season, int year)
        {
            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join ac in db.AssignmentCategories on c.CId equals ac.CId into xac
                            from assignCat in xac.DefaultIfEmpty()
                            where x.Department == subject 
                            && x.CNum == num 
                            && c.Semester == season
                            && c.Year == year 
                            select new {name = assignCat.CatName, weight = assignCat.GrdWeight};

            return Json(query.ToArray());
        }

        /// <summary>
        /// Creates a new assignment category for the specified class.
        /// If a category of the given class with the given name already exists, return success = false.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The new category name</param>
        /// <param name="catweight">The new category weight</param>
        /// <returns>A JSON object containing {success = true/false} </returns>
        public IActionResult CreateAssignmentCategory(string subject, int num, string season, int year, string category, int catweight)
        {

            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            where x.Department == subject 
                            && x.CNum == num 
                            && c.Semester == season
                            && c.Year == year 
                            select new {cid = c.CId};


            uint cID = 0 ;
            foreach(var v in query)
            {
                cID = v.cid;
            }

            try
            {
                AssignmentCategory cat = new AssignmentCategory();
                cat.CatName = category;
                cat.CId = cID;
                cat.GrdWeight = (uint) catweight;
                db.AssignmentCategories.Add(cat);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });

            }

        }

        /// <summary>
        /// Creates a new assignment for the given class and category.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="asgpoints">The max point value for the new assignment</param>
        /// <param name="asgdue">The due DateTime for the new assignment</param>
        /// <param name="asgcontents">The contents of the new assignment</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult CreateAssignment(string subject, int num, string season, int year, string category, string asgname, int asgpoints, DateTime asgdue, string asgcontents)
        {

            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join ac in db.AssignmentCategories on c.CId equals ac.CId into xac
                            from assignCat in xac.DefaultIfEmpty()
                            where x.Department == subject 
                            && x.CNum == num 
                            && c.Semester == season
                            && c.Year == year 
                            && assignCat.CatName == category
                            select new {acid = assignCat.AcId};

            int aCID = 0;
            foreach(var v in query)
            {
                aCID = v.acid;
            }
            try
            {
                Assignment assignm = new Assignment();
                assignm.AName = asgname;
                assignm.AcId = aCID;
                assignm.DueDate = asgdue;
                assignm.Instructions = asgcontents;
                assignm.MaxPoints = (uint) asgpoints;
                db.Assignments.Add(assignm);
                db.SaveChanges();
                return Json(new { success = true });

            }
            catch
            {
                return Json(new { success = false });

            }
            
        }


        /// <summary>
        /// Gets a JSON array of all the submissions to a certain assignment.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "time" - DateTime of the submission
        /// "score" - The score given to the submission
        /// 
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetSubmissionsToAssignment(string subject, int num, string season, int year, string category, string asgname)
        {
            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join ac in db.AssignmentCategories on c.CId equals ac.CId into xac
                            from assignCat in xac.DefaultIfEmpty()
                            join a in db.Assignments on assignCat.AcId equals a.AcId into aasignCat
                            from assignment in aasignCat.DefaultIfEmpty()
                            join s in db.Submissions on assignment.AId equals s.AId into sassign
                            from submission in sassign.DefaultIfEmpty()
                            join stu in db.Students on submission.Student equals stu.UId into sstu
                            from student in sstu.DefaultIfEmpty()
                            where x.Department == subject 
                            && x.CNum == num 
                            && c.Semester == season
                            && c.Year == year 
                            && assignCat.CatName == category
                            && assignment.AName == asgname
                            select new {fname = student.FirstName, lname = student.LastName, uid = student.UId, time = submission.Time, score = submission.Score};

            return Json(query.ToArray());
        }


        /// <summary>
        /// Set the score of an assignment submission
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <param name="uid">The uid of the student who's submission is being graded</param>
        /// <param name="score">The new score for the submission</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult GradeSubmission(string subject, int num, string season, int year, string category, string asgname, string uid, int score)
        {

            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join ac in db.AssignmentCategories on c.CId equals ac.CId into xac
                            from assignCat in xac.DefaultIfEmpty()
                            join a in db.Assignments on assignCat.AcId equals a.AcId into aasignCat
                            from assignment in aasignCat.DefaultIfEmpty()
                            join s in db.Submissions on assignment.AId equals s.AId into sassign
                            from submission in sassign.DefaultIfEmpty()
                            join stu in db.Students on submission.Student equals stu.UId into sstu
                            from student in sstu.DefaultIfEmpty()
                            where x.Department == subject 
                            && x.CNum == num 
                            && c.Semester == season
                            && c.Year == year 
                            && assignCat.CatName == category
                            && assignment.AName == asgname
                            && student.UId == uid
                            select new {aid = submission.AId, stuID = submission.Student};
            
            uint AID = 0;
            string uID = "";
            foreach(var v in query)
            {
                AID = v.aid;
                uID = v.stuID;
            }

            try
            {
                Submission s = db.Submissions.FirstOrDefault(x => x.AId == AID && x.Student == uID);
                
                s.Score = (uint) score;

                db.SaveChanges();
                return Json(new { success = true });

            }catch
            {
                return Json(new { success = false });

            }




        }


        /// <summary>
        /// Returns a JSON array of the classes taught by the specified professor
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester in which the class is taught
        /// "year" - The year part of the semester in which the class is taught
        /// </summary>
        /// <param name="uid">The professor's uid</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {      

            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from courses in ccr.DefaultIfEmpty()
                            join p in db.Professors on c.Professor equals p.UId into cp
                            from professor in cp.DefaultIfEmpty()
                            where professor.UId == uid
                            select new { subject = courses.Department, number = courses.CNum, name = courses.CName, season = c.Semester, year = c.Year };
                        
            return Json(query.ToArray());
        }
     
        /*******End code to modify********/
    }
}

