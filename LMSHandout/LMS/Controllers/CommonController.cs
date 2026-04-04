using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS.Controllers
{
    public class CommonController : Controller
    {
        private readonly LMSContext db;

        public CommonController(LMSContext _db)
        {
            db = _db;
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Retreive a JSON array of all departments from the database.
        /// Each object in the array should have a field called "name" and "subject",
        /// where "name" is the department name and "subject" is the subject abbreviation.
        /// </summary>
        /// <returns>The JSON array</returns>
        public IActionResult GetDepartments()
        {            

            var query = from dep in db.Departments
                                         select new {name = dep.DName, subject = dep.Abbreviation};


            return Json(query.ToArray());
        }



        /// <summary>
        /// Returns a JSON array representing the course catalog.
        /// Each object in the array should have the following fields:
        /// "subject": The subject abbreviation, (e.g. "CS")
        /// "dname": The department name, as in "Computer Science"
        /// "courses": An array of JSON objects representing the courses in the department.
        ///            Each field in this inner-array should have the following fields:
        ///            "number": The course number (e.g. 5530)
        ///            "cname": The course name (e.g. "Database Systems")
        /// </summary>
        /// <returns>The JSON array</returns>
        public IActionResult GetCatalog()
        {            
            var query = from c in db.Courses
                            join d in db.Departments on c.Department equals d.Abbreviation into cd
                            from z in cd.DefaultIfEmpty()
                            select new {subject = z.Abbreviation, dname = z.DName, cname = c.CName, number = c.CNum};


            return Json(query.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all class offerings of a specific course.
        /// Each object in the array should have the following fields:
        /// "season": the season part of the semester, such as "Fall"
        /// "year": the year part of the semester
        /// "location": the location of the class
        /// "start": the start time in format "hh:mm:ss"
        /// "end": the end time in format "hh:mm:ss"
        /// "fname": the first name of the professor
        /// "lname": the last name of the professor
        /// </summary>
        /// <param name="subject">The subject abbreviation, as in "CS"</param>
        /// <param name="number">The course number, as in 5530</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetClassOfferings(string subject, int number)
        {            
            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join p in db.Professors on c.Professor equals p.UId into xp
                            from y in xp.DefaultIfEmpty()
                            where x.Department == subject && x.CNum == number
                            select new {season = c.Semester, year = c.Year, location = c.Location, start = c.StartTime, end = c.EndTime, fname = y.FirstName, lname = y.LastName};


            return Json(query.ToArray());
        }

        /// <summary>
        /// This method does NOT return JSON. It returns plain text (containing html).
        /// Use "return Content(...)" to return plain text.
        /// Returns the contents of an assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param> // Courses
        /// <param name="num">The course number</param> // Courses
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param> // Classes
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param> // Classes
        /// <param name="category">The name of the assignment category in the class</param> // AssCat
        /// <param name="asgname">The name of the assignment in the category</param> //Assignments
        /// <returns>The assignment contents</returns>
        public IActionResult GetAssignmentContents(string subject, int num, string season, int year, string category, string asgname)
        {            

            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join ac in db.AssignmentCategories on c.CId equals ac.CId into xac
                            from y in xac.DefaultIfEmpty()
                            join a in db.Assignments on y.AcId equals a.AcId into ya
                            from z in ya.DefaultIfEmpty()
                            where x.Department == subject && x.CNum == num && c.Semester == season && c.Year == year && y.CatName == category && z.AName == asgname
                            select new {content = z.Instructions};


            return Content(query.ToString());
        }


        /// <summary>
        /// This method does NOT return JSON. It returns plain text (containing html).
        /// Use "return Content(...)" to return plain text.
        /// Returns the contents of an assignment submission.
        /// Returns the empty string ("") if there is no submission.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param> \\ Courses
        /// <param name="num">The course number</param> \\Courses
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param> \\Classes
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param> \\Classes
        /// <param name="category">The name of the assignment category in the class</param> \\AssCat
        /// <param name="asgname">The name of the assignment in the category</param> \\Assignments
        /// <param name="uid">The uid of the student who submitted it</param> \\ Submission
        /// <returns>The submission text</returns>
        public IActionResult GetSubmissionText(string subject, int num, string season, int year, string category, string asgname, string uid)
        {            
            var query = from c in db.Classes
                            join cr in db.Courses on c.CrId equals cr.CrId into ccr
                            from x in ccr.DefaultIfEmpty()
                            join ac in db.AssignmentCategories on c.CId equals ac.CId into xac
                            from y in xac.DefaultIfEmpty()
                            join a in db.Assignments on y.AcId equals a.AcId into ya
                            from z in ya.DefaultIfEmpty()
                            join s in db.Submissions on  z.AId equals s.AId into sz
                            from w in sz.DefaultIfEmpty()
                            where x.Department == subject && x.CNum == num && c.Semester == season && c.Year == year && y.CatName == category && z.AName == asgname && w.Student == uid
                            select new {content = w.StudentSolution};


            return Content(query.ToString());
        }


        /// <summary>
        /// Gets information about a user as a single JSON object.
        /// The object should have the following fields:
        /// "fname": the user's first name
        /// "lname": the user's last name
        /// "uid": the user's uid
        /// "department": (professors and students only) the name (such as "Computer Science") of the department for the user. 
        ///               If the user is a Professor, this is the department they work in.
        ///               If the user is a Student, this is the department they major in.    
        ///               If the user is an Administrator, this field is not present in the returned JSON
        /// </summary>
        /// <param name="uid">The ID of the user</param>
        /// <returns>
        /// The user JSON object 
        /// or an object containing {success: false} if the user doesn't exist
        /// </returns>
        public IActionResult GetUser(string uid)
        {     
             var query = from students in db.Students 
                            join p in db.Professors on students.UId equals p.UId into ps
                            from profStu in ps.DefaultIfEmpty()
                            join a in db.Administrators on profStu.UId equals a.UId into psa
                            from people in psa.DefaultIfEmpty()
                            join d1 in db.Departments on students.Major equals d1.Abbreviation into dept1 
                            from dept2 in dept1.DefaultIfEmpty()
                            join d2 in db.Departments on profStu.Department equals d2.Abbreviation into dept3
                            from dept in dept3.DefaultIfEmpty()
                            select new {fname =  people.FirstName, lname = people.LastName, uid = people.UId, department = dept.DName == null ? "" : dept.DName};

            return Json(new { success = false });
        }


        /*******End code to modify********/
    }
}

