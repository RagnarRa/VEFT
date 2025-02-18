﻿using System.Collections.Generic;
using System.Linq;
using CoursesAPI.Models;
using CoursesAPI.Services.Exceptions;
using CoursesAPI.Services.Models.Entities;
using CoursesAPI.Services.Services;
using CoursesAPI.Tests.MockObjects;
using CoursesAPI.Tests.TestExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursesAPI.Tests.Services
{
	[TestClass]
	public class CourseServicesTests
	{
		private MockUnitOfWork<MockDataContext> _mockUnitOfWork;
		private CoursesServiceProvider _service;
		private List<TeacherRegistration> _teacherRegistrations;

		private const string SSN_DABS    = "1203735289";
		private const string SSN_GUNNA   = "1234567890";
		private const string INVALID_SSN = "9876543210";

		private const string NAME_GUNNA  = "Guðrún Guðmundsdóttir";

		private const int COURSEID_VEFT_20153 = 1337;
		private const int COURSEID_VEFT_20163 = 1338;
        private const int COURSEID_TSAM_20153 = 1339;
        private const int COURSEID_HONN_20153 = 1340;
		private const int INVALID_COURSEID    = 9999;

        private const int NUM_COURSES_20153 = 3; 

		[TestInitialize]
		public void Setup()
		{
			_mockUnitOfWork = new MockUnitOfWork<MockDataContext>();

			#region Persons
			var persons = new List<Person>
			{
				// Of course I'm the first person,
				// did you expect anything else?
				new Person
				{
					ID    = 1,
					Name  = "Daníel B. Sigurgeirsson",
					SSN   = SSN_DABS,
					Email = "dabs@ru.is"
				},
				new Person
				{
					ID    = 2,
					Name  = NAME_GUNNA,
					SSN   = SSN_GUNNA,
					Email = "gunna@ru.is"
				}
			};
			#endregion

			#region Course templates

			var courseTemplates = new List<CourseTemplate>
			{
				new CourseTemplate
				{
					CourseID    = "T-514-VEFT",
					Description = "Í þessum áfanga verður fjallað um vefþj...",
					Name        = "Vefþjónustur"
				},
                new CourseTemplate
                {
                    CourseID     = "T-409-TSAM",
                    Description  = "Í þessum áfanga verður fjallað um tölvusamskipti..",
                    Name         = "Tölvusamskipti"
                },
                new CourseTemplate
                {
                    CourseID    = "T-302-HONN",
                    Description = "Í þessum áfanga..",
                    Name        = "Hönnun og smíði hugbúnaðar"
                }
			};
			#endregion

			#region Courses
			var courses = new List<CourseInstance>
			{
				new CourseInstance
				{
					ID         = COURSEID_VEFT_20153,
					CourseID   = "T-514-VEFT",
					SemesterID = "20153"
				},
				new CourseInstance
				{
					ID         = COURSEID_VEFT_20163,
					CourseID   = "T-514-VEFT",
					SemesterID = "20163"
				},
                new CourseInstance
                {
                    ID = COURSEID_TSAM_20153,
                    CourseID = "T-409-TSAM",
                    SemesterID = "20153"
                },
                new CourseInstance
                {
                    ID = COURSEID_HONN_20153,
                    CourseID = "T-302-HONN",
                    SemesterID = "20153"
                }
			};
			#endregion

			#region Teacher registrations
			_teacherRegistrations = new List<TeacherRegistration>
			{
				new TeacherRegistration
				{
					ID               = 101,
					CourseInstanceID = COURSEID_VEFT_20153,
					SSN              = SSN_DABS,
					Type             = TeacherType.MainTeacher
				},
                new TeacherRegistration
                {
                    ID               = 102,
                    CourseInstanceID = COURSEID_HONN_20153,
                    SSN              = SSN_GUNNA,
                    Type             = TeacherType.MainTeacher
                }
			};
			#endregion

			_mockUnitOfWork.SetRepositoryData(persons);
			_mockUnitOfWork.SetRepositoryData(courseTemplates);
			_mockUnitOfWork.SetRepositoryData(courses);
			_mockUnitOfWork.SetRepositoryData(_teacherRegistrations);

			// TODO: this would be the correct place to add 
			// more mock data to the mockUnitOfWork!

			_service = new CoursesServiceProvider(_mockUnitOfWork);
		}

		#region GetCoursesBySemester
		/// <summary>
		/// Tests that when checking for a semester with no courses, the empty list is returned.
		/// </summary>
		[TestMethod]
		public void GetCoursesBySemester_ReturnsEmptyListWhenNoDataDefined()
		{
			// Arrange:

			// Act:
            List<CourseInstanceDTO> dto = _service.GetCourseInstancesBySemester("20173");
			// Assert:
            Assert.IsNotNull(dto);
            Assert.AreEqual(0, dto.Count);
		}

        /// <summary>
        /// In this test, we assert that the result set from giving the
        /// GetCourseInstancesBySemester function no arguments is the same
        /// as the result set received when giving it the argument 20153.
        /// </summary>
        [TestMethod]
        public void GetCoursesBySemester_NoSemester()
        {
            //Arrange:
            var coursesBy2015 = _service.GetCourseInstancesBySemester("20153"); //Get the actual results for 2015.. 
            //Act:
            //No semester given
            var courses = _service.GetCourseInstancesBySemester();

            //Assert:
            Assert.AreEqual(courses.Count, coursesBy2015.Count);
            Assert.AreEqual(courses.Count, NUM_COURSES_20153); 
            for (int i = 0; i < courses.Count; i++)
            {
                Assert.AreEqual(courses[i].CourseInstanceID, coursesBy2015[i].CourseInstanceID);
            }
        }

        /// <summary>
        /// In this test, we assert that when given the argument 20153, it gets exactly the courses
        /// taught in that semester. No more, no less.
        /// </summary>
        public void GetCoursesBySemester_NoMoreNoLess()
        {
            //Arrange:
            //Act:
            var courses = _service.GetCourseInstancesBySemester("20153");
            //Assert:
            Assert.AreEqual(3, courses.Count);
        }

        /// <summary>
        /// Tests that each course returned includes a MainTeacher or an empty string for it.
        /// </summary>
        [TestMethod]
        public void GetCoursesBySemester_CoursesIncludeMainTeacher()
        {
            //Arrange:
            //Act:
            var courses = _service.GetCourseInstancesBySemester("20153");
            //Assert:
            foreach (CourseInstanceDTO courseInstance in courses)
            {
                //If the mainteacher is not null.. then presumably it is either the empty string or a real teacher
                Assert.IsNotNull(courseInstance.MainTeacher);
            }
        }

        /// <summary>
        /// Tests that a course with no main teacher has the main teacher returned as an empty string.
        /// </summary>
        [TestMethod]
        public void GetCoursesBySemester_CourseWithNoMainTeacher()
        {
            //Arrange:
            //Act:
            var courses = _service.GetCourseInstancesBySemester("20153");
            //Assert: 
            foreach (CourseInstanceDTO courseInstance in courses)
            {
                if (courseInstance.CourseInstanceID == COURSEID_TSAM_20153)
                {
                    Assert.AreEqual("", courseInstance.MainTeacher);
                    break;
                }
            }
        }

        /// <summary>
        /// Tests that a course with a main teacher returns his/her name.
        /// </summary>
        [TestMethod]
        public void GetCoursesBySemester_CourseWithMainTeacher()
        {
            //Arrange: 
            //Act: 
            var courses = _service.GetCourseInstancesBySemester("20153");

            //Assert: 
            foreach (CourseInstanceDTO courseInstance in courses)
            {
                if (courseInstance.CourseInstanceID == COURSEID_HONN_20153)
                {
                    Assert.AreEqual(NAME_GUNNA, courseInstance.MainTeacher);
                    break;
                }
            }
        }

		#endregion

		#region AddTeacher

		/// <summary>
		/// Adds a main teacher to a course which doesn't have a
		/// main teacher defined already (see test data defined above).
		/// </summary>
		[TestMethod]
		public void AddTeacher_WithValidTeacherAndCourse()
		{
			// Arrange:
			var model = new AddTeacherViewModel
			{
				SSN  = SSN_GUNNA,
				Type = TeacherType.MainTeacher
			};
			var prevCount = _teacherRegistrations.Count;
			// Note: the method uses test data defined in [TestInitialize]

			// Act:
			var dto = _service.AddTeacherToCourse(COURSEID_VEFT_20163, model);

			// Assert:

			// Check that the dto object is correctly populated:
			Assert.AreEqual(SSN_GUNNA, dto.SSN);
			Assert.AreEqual(NAME_GUNNA, dto.Name);

			// Ensure that a new entity object has been created:
			var currentCount = _teacherRegistrations.Count;
			Assert.AreEqual(prevCount + 1, currentCount);

			// Get access to the entity object and assert that
			// the properties have been set:
			var newEntity = _teacherRegistrations.Last();
			Assert.AreEqual(COURSEID_VEFT_20163, newEntity.CourseInstanceID);
			Assert.AreEqual(SSN_GUNNA, newEntity.SSN);
			Assert.AreEqual(TeacherType.MainTeacher, newEntity.Type);

			// Ensure that the Unit Of Work object has been instructed
			// to save the new entity object:
			Assert.IsTrue(_mockUnitOfWork.GetSaveCallCount() > 0);
		}

		[TestMethod]
		[ExpectedException(typeof(AppObjectNotFoundException))]
		public void AddTeacher_InvalidCourse()
		{
			// Arrange:
			var model = new AddTeacherViewModel
			{
				SSN  = SSN_GUNNA,
				Type = TeacherType.AssistantTeacher
			};
			// Note: the method uses test data defined in [TestInitialize]

			// Act:
			_service.AddTeacherToCourse(INVALID_COURSEID, model);
		}

		/// <summary>
		/// Ensure it is not possible to add a person as a teacher
		/// when that person is not registered in the system.
		/// </summary>
		[TestMethod]
		[ExpectedException(typeof (AppObjectNotFoundException))]
		public void AddTeacher_InvalidTeacher()
		{
			// Arrange:
			var model = new AddTeacherViewModel
			{
				SSN  = INVALID_SSN,
				Type = TeacherType.MainTeacher
			};
			// Note: the method uses test data defined in [TestInitialize]

			// Act:
			_service.AddTeacherToCourse(COURSEID_VEFT_20153, model);
		}

		/// <summary>
		/// In this test, we test that it is not possible to
		/// add another main teacher to a course, if one is already
		/// defined.
		/// </summary>
        /// The exception message should be course_already... 
		[TestMethod]
		[ExpectedExceptionWithMessage(typeof (AppValidationException), "COURSE_ALREADY_HAS_A_MAIN_TEACHER")]
		public void AddTeacher_AlreadyWithMainTeacher()
		{
			// Arrange:
			var model = new AddTeacherViewModel
			{
				SSN  = SSN_GUNNA,
				Type = TeacherType.MainTeacher
			};
			// Note: the method uses test data defined in [TestInitialize]

			// Act:
			_service.AddTeacherToCourse(COURSEID_VEFT_20153, model);
		}

		/// <summary>
		/// In this test, we ensure that a person cannot be added as a
		/// teacher in a course, if that person is already registered
		/// as a teacher in the given course.
		/// </summary>
		[TestMethod]
		[ExpectedExceptionWithMessage(typeof (AppValidationException), "PERSON_ALREADY_REGISTERED_TEACHER_IN_COURSE")]
		public void AddTeacher_PersonAlreadyRegisteredAsTeacherInCourse()
		{
			// Arrange:
			var model = new AddTeacherViewModel
			{
				SSN  = SSN_DABS,
				Type = TeacherType.AssistantTeacher
			};
			// Note: the method uses test data defined in [TestInitialize]

			// Act:
			_service.AddTeacherToCourse(COURSEID_VEFT_20153, model);
		}

		#endregion
	}
}
