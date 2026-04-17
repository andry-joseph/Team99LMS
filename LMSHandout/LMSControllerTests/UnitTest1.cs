using LMS.Controllers;
using LMS.Models.LMSModels;
using LMS_CustomIdentity.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace LMSControllerTests
{
    public class UnitTest1
    {
        [Fact]
        public void GetDepartments_ReturnsSeededDepartments()
        {
            using var db = MakeSampleDB();
            var ctrl = new CommonController(db);

            using var json = ToJson(ctrl.GetDepartments());
            var departments = json.RootElement.EnumerateArray().ToList();

            Assert.Equal(2, departments.Count);
            Assert.Contains(departments, d =>
                d.GetProperty("subject").GetString() == "CS" &&
                d.GetProperty("name").GetString() == "Computer Science");
        }

        [Fact]
        public void GetCatalog_GroupsCoursesByDepartment()
        {
            using var db = MakeSampleDB();
            var ctrl = new CommonController(db);

            using var json = ToJson(ctrl.GetCatalog());
            var departments = json.RootElement.EnumerateArray().ToList();
            var cs = departments.Single(d => d.GetProperty("subject").GetString() == "CS");

            Assert.Equal("Computer Science", cs.GetProperty("dname").GetString());
            Assert.Equal(2, cs.GetProperty("courses").GetArrayLength());
        }

        [Fact]
        public void GetClassOfferings_ReturnsProfessorAndMeetingInfo()
        {
            using var db = MakeSampleDB();
            var ctrl = new CommonController(db);

            using var json = ToJson(ctrl.GetClassOfferings("CS", 5530));
            var offerings = json.RootElement.EnumerateArray().ToList();
            var offering = Assert.Single(offerings);

            Assert.Equal("Fall", offering.GetProperty("season").GetString());
            Assert.Equal("Ada", offering.GetProperty("fname").GetString());
            Assert.Equal("Lovelace", offering.GetProperty("lname").GetString());
        }

        [Fact]
        public void GetAssignmentContents_ReturnsInstructions()
        {
            using var db = MakeSampleDB();
            var ctrl = new CommonController(db);

            var result = Assert.IsType<ContentResult>(
                ctrl.GetAssignmentContents("CS", 5530, "Fall", 2026, "Homework", "HW1"));

            Assert.Equal("<p>Write SQL</p>", result.Content);
        }

        [Fact]
        public void GetSubmissionText_ReturnsEmptyStringWhenMissing()
        {
            using var db = MakeSampleDB();
            var ctrl = new CommonController(db);

            var result = Assert.IsType<ContentResult>(
                ctrl.GetSubmissionText("CS", 5530, "Fall", 2026, "Homework", "HW2", "u0000002"));

            Assert.Equal("", result.Content);
        }

        [Fact]
        public void GetUser_ReturnsStudentInfo()
        {
            using var db = MakeSampleDB();
            var ctrl = new CommonController(db);

            using var json = ToJson(ctrl.GetUser("u0000002"));
            var root = json.RootElement;

            Assert.Equal("Alan", root.GetProperty("fname").GetString());
            Assert.Equal("Turing", root.GetProperty("lname").GetString());
            Assert.Equal("Computer Science", root.GetProperty("department").GetString());
        }

        [Fact]
        public void GetCourses_ReturnsDepartmentCourses()
        {
            using var db = MakeSampleDB();
            var ctrl = new AdministratorController(db);

            using var json = ToJson(ctrl.GetCourses("CS"));
            var courses = json.RootElement.EnumerateArray().ToList();

            Assert.Equal(2, courses.Count);
            Assert.Contains(courses, c => c.GetProperty("number").GetUInt32() == 5530);
        }

        [Fact]
        public void GetProfessors_ReturnsDepartmentProfessors()
        {
            using var db = MakeSampleDB();
            var ctrl = new AdministratorController(db);

            using var json = ToJson(ctrl.GetProfessors("CS"));
            var professors = json.RootElement.EnumerateArray().ToList();
            var professor = Assert.Single(professors);

            Assert.Equal("Ada", professor.GetProperty("fname").GetString());
            Assert.Equal("u0000001", professor.GetProperty("uid").GetString());
        }

        [Fact]
        public void CreateCourse_RejectsDuplicateCourse()
        {
            using var db = MakeSampleDB();
            var ctrl = new AdministratorController(db);

            using var json = ToJson(ctrl.CreateCourse("CS", 5530, "Duplicate Databases"));

            Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        }

        [Fact]
        public void CreateClass_RejectsLocationConflict()
        {
            using var db = MakeSampleDB();
            var ctrl = new AdministratorController(db);

            using var json = ToJson(ctrl.CreateClass(
                "MATH", 1210, "Fall", 2026,
                new DateTime(2026, 9, 1, 9, 30, 0),
                new DateTime(2026, 9, 1, 10, 45, 0),
                "WEB 123", "u0000004"));

            Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        }

        [Fact]
        public void GetMyClasses_ReturnsStudentClasses()
        {
            using var db = MakeSampleDB();
            var ctrl = new StudentController(db);

            using var json = ToJson(ctrl.GetMyClasses("u0000002"));
            var classes = json.RootElement.EnumerateArray().ToList();

            Assert.Equal(2, classes.Count);
            Assert.Contains(classes, c => c.GetProperty("grade").GetString() == "A");
            Assert.Contains(classes, c => c.GetProperty("grade").GetString() == "--");
        }

        [Fact]
        public void GetAssignmentsInClass_ReturnsNullScoreWhenStudentHasNotSubmitted()
        {
            using var db = MakeSampleDB();
            var ctrl = new StudentController(db);

            using var json = ToJson(ctrl.GetAssignmentsInClass("CS", 5530, "Fall", 2026, "u0000002"));
            var assignments = json.RootElement.EnumerateArray().ToList();
            var hw2 = assignments.Single(a => a.GetProperty("aname").GetString() == "HW2");

            Assert.Equal(2, assignments.Count);
            Assert.Equal(JsonValueKind.Null, hw2.GetProperty("score").ValueKind);
        }

        [Fact]
        public void SubmitAssignmentText_UpdatesExistingSubmission()
        {
            using var db = MakeSampleDB();
            var ctrl = new StudentController(db);

            using var json = ToJson(ctrl.SubmitAssignmentText(
                "CS", 5530, "Fall", 2026, "Homework", "HW1", "u0000002", "updated solution"));

            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(
                "updated solution",
                db.Submissions.Single(s => s.AId == 1 && s.Student == "u0000002").StudentSolution);
        }

        [Fact]
        public void Enroll_AddsStudentToClass()
        {
            using var db = MakeSampleDB();
            var ctrl = new StudentController(db);

            using var json = ToJson(ctrl.Enroll("MATH", 1210, "Fall", 2026, "u0000002"));

            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Contains(db.Enrollments, e => e.CId == 3 && e.Student == "u0000002");
        }

        [Fact]
        public void GetGPA_IgnoresUngradedClasses()
        {
            using var db = MakeSampleDB();
            var ctrl = new StudentController(db);

            using var json = ToJson(ctrl.GetGPA("u0000002"));

            Assert.Equal(4.0, json.RootElement.GetProperty("gpa").GetDouble());
        }

        [Fact]
        public void GetStudentsInClass_ReturnsEnrolledStudents()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.GetStudentsInClass("CS", 5530, "Fall", 2026));
            var students = json.RootElement.EnumerateArray().ToList();

            Assert.Equal(2, students.Count);
            Assert.Contains(students, s => s.GetProperty("uid").GetString() == "u0000002");
        }

        [Fact]
        public void GetAssignmentsInCategory_ReturnsSubmissionCounts()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.GetAssignmentsInCategory("CS", 5530, "Fall", 2026, "Homework"));
            var assignments = json.RootElement.EnumerateArray().ToList();
            var hw1 = assignments.Single(a => a.GetProperty("aname").GetString() == "HW1");
            var hw2 = assignments.Single(a => a.GetProperty("aname").GetString() == "HW2");

            Assert.Equal(1, hw1.GetProperty("submissions").GetInt32());
            Assert.Equal(0, hw2.GetProperty("submissions").GetInt32());
        }

        [Fact]
        public void GetAssignmentCategories_ReturnsWeights()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.GetAssignmentCategories("CS", 5530, "Fall", 2026));
            var categories = json.RootElement.EnumerateArray().ToList();

            Assert.Contains(categories, c =>
                c.GetProperty("name").GetString() == "Homework" &&
                c.GetProperty("weight").GetUInt32() == 50);
        }

        [Fact]
        public void CreateAssignmentCategory_AddsCategory()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.CreateAssignmentCategory("CS", 5530, "Fall", 2026, "Labs", 10));

            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Contains(db.AssignmentCategories, c => c.CatName == "Labs" && c.CId == 1);
        }

        [Fact]
        public void CreateAssignment_AddsAssignment()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.CreateAssignment(
                "CS", 5530, "Fall", 2026, "Homework", "HW3", 25,
                new DateTime(2026, 10, 1, 23, 59, 0), "<p>More SQL</p>"));

            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Contains(db.Assignments, a => a.AName == "HW3" && a.AcId == 1);
        }

        [Fact]
        public void CreateAssignment_RecomputesGradesForEnrolledStudents()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.CreateAssignment(
                "CS", 5530, "Fall", 2026, "Homework", "HW3", 25,
                new DateTime(2026, 10, 1, 23, 59, 0), "<p>More SQL</p>"));

            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("E", db.Enrollments.Single(e => e.CId == 1 && e.Student == "u0000002").Grade);
            Assert.Equal("E", db.Enrollments.Single(e => e.CId == 1 && e.Student == "u0000003").Grade);
        }

        [Fact]
        public void GetSubmissionsToAssignment_ReturnsSubmittedStudents()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.GetSubmissionsToAssignment("CS", 5530, "Fall", 2026, "Homework", "HW1"));
            var submissions = json.RootElement.EnumerateArray().ToList();
            var submission = Assert.Single(submissions);

            Assert.Equal("u0000002", submission.GetProperty("uid").GetString());
            Assert.Equal(95, submission.GetProperty("score").GetInt32());
        }

        [Fact]
        public void GradeSubmission_UpdatesSubmissionScore()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.GradeSubmission("CS", 5530, "Fall", 2026, "Homework", "HW1", "u0000002", 88));

            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal((uint?)88, db.Submissions.Single(s => s.AId == 1 && s.Student == "u0000002").Score);
        }

        [Fact]
        public void GradeSubmission_RecomputesStudentEnrollmentGrade()
        {
            using var db = MakeSampleDB();
            db.Submissions.Add(
                new Submission
                {
                    AId = 2,
                    Student = "u0000002",
                    Time = new DateTime(2026, 9, 21, 18, 0, 0),
                    StudentSolution = "Second homework",
                    Score = null
                });
            db.SaveChanges();

            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.GradeSubmission("CS", 5530, "Fall", 2026, "Homework", "HW2", "u0000002", 100));

            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("A", db.Enrollments.Single(e => e.CId == 1 && e.Student == "u0000002").Grade);
        }

        [Fact]
        public void GetMyClasses_ReturnsProfessorClasses()
        {
            using var db = MakeSampleDB();
            var ctrl = new ProfessorController(db);

            using var json = ToJson(ctrl.GetMyClasses("u0000001"));
            var classes = json.RootElement.EnumerateArray().ToList();

            Assert.Equal(2, classes.Count);
            Assert.Contains(classes, c => c.GetProperty("number").GetUInt32() == 5530);
        }

        private static JsonDocument ToJson(IActionResult result)
        {
            var json = Assert.IsType<JsonResult>(result);
            return JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        }

        private static LMSContext MakeSampleDB()
        {
            var contextOptions = new DbContextOptionsBuilder<LMSContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .UseApplicationServiceProvider(NewServiceProvider())
                .Options;

            var db = new LMSContext(contextOptions);

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Departments.AddRange(
                new Department { DName = "Computer Science", Abbreviation = "CS" },
                new Department { DName = "Mathematics", Abbreviation = "MATH" });

            db.Professors.AddRange(
                new Professor
                {
                    UId = "u0000001",
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Dob = new DateOnly(1815, 12, 10),
                    Department = "CS"
                },
                new Professor
                {
                    UId = "u0000004",
                    FirstName = "Isaac",
                    LastName = "Newton",
                    Dob = new DateOnly(1643, 1, 4),
                    Department = "MATH"
                });

            db.Students.AddRange(
                new Student
                {
                    UId = "u0000002",
                    FirstName = "Alan",
                    LastName = "Turing",
                    Dob = new DateOnly(1912, 6, 23),
                    Major = "CS"
                },
                new Student
                {
                    UId = "u0000003",
                    FirstName = "Grace",
                    LastName = "Hopper",
                    Dob = new DateOnly(1906, 12, 9),
                    Major = "MATH"
                });

            db.Administrators.Add(
                new Administrator
                {
                    UId = "u0000005",
                    FirstName = "Admin",
                    LastName = "User",
                    Dob = new DateOnly(1980, 1, 1)
                });

            db.Courses.AddRange(
                new Course { CrId = 1, Department = "CS", CNum = 5530, CName = "Databases" },
                new Course { CrId = 2, Department = "CS", CNum = 3500, CName = "Software Practice" },
                new Course { CrId = 3, Department = "MATH", CNum = 1210, CName = "Calculus I" });

            db.Classes.AddRange(
                new Class
                {
                    CId = 1,
                    CrId = 1,
                    Semester = "Fall",
                    Year = 2026,
                    Professor = "u0000001",
                    Location = "WEB 123",
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 15)
                },
                new Class
                {
                    CId = 2,
                    CrId = 2,
                    Semester = "Spring",
                    Year = 2027,
                    Professor = "u0000001",
                    Location = "WEB 201",
                    StartTime = new TimeOnly(11, 0),
                    EndTime = new TimeOnly(12, 15)
                },
                new Class
                {
                    CId = 3,
                    CrId = 3,
                    Semester = "Fall",
                    Year = 2026,
                    Professor = "u0000004",
                    Location = "JTB 110",
                    StartTime = new TimeOnly(13, 0),
                    EndTime = new TimeOnly(14, 15)
                });

            db.AssignmentCategories.AddRange(
                new AssignmentCategory { AcId = 1, CId = 1, CatName = "Homework", GrdWeight = 50 },
                new AssignmentCategory { AcId = 2, CId = 1, CatName = "Exams", GrdWeight = 50 });

            db.Assignments.AddRange(
                new Assignment
                {
                    AId = 1,
                    AcId = 1,
                    AName = "HW1",
                    DueDate = new DateTime(2026, 9, 15, 23, 59, 0),
                    Instructions = "<p>Write SQL</p>",
                    MaxPoints = 100
                },
                new Assignment
                {
                    AId = 2,
                    AcId = 1,
                    AName = "HW2",
                    DueDate = new DateTime(2026, 9, 22, 23, 59, 0),
                    Instructions = "<p>Normalize tables</p>",
                    MaxPoints = 100
                });

            db.Enrollments.AddRange(
                new Enrollment { CId = 1, Student = "u0000002", Grade = "A" },
                new Enrollment { CId = 2, Student = "u0000002", Grade = null },
                new Enrollment { CId = 1, Student = "u0000003", Grade = "B" });

            db.Submissions.Add(
                new Submission
                {
                    AId = 1,
                    Student = "u0000002",
                    Time = new DateTime(2026, 9, 14, 20, 0, 0),
                    StudentSolution = "SELECT * FROM Courses;",
                    Score = 95
                });

            db.SaveChanges();

            return db;
        }

        private static ServiceProvider NewServiceProvider()
        {
            return new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
        }
    }
}
