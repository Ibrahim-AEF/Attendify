using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presistance.Data;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation
{
    [Authorize(Roles = "Admin")]
    [Route("api/students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Student> _passwordHasher;

        public StudentsController(AppDbContext context , IPasswordHasher<Student> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterStudent([FromBody] StudentDto studentDto)
        {
            if (await _context.Students.AnyAsync(s => s.StudentId == studentDto.StudentId))
            {
                return BadRequest(new { message = "Student ID already exists" });
            }

            if (await _context.Students.AnyAsync(s => s.Email == studentDto.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var instructor = await _context.Instructors.FindAsync(studentDto.InstructorId);
            if (instructor == null)
            {
                return BadRequest(new { message = "Instructor not found" });
            }

            var student = new Student
            {
                Id=studentDto.Id,
                StudentId = studentDto.StudentId,
                FirstName = studentDto.FirstName,
                LastName = studentDto.LastName,
                Email = studentDto.Email,
                Phone = studentDto.Phone,
                CGPA = studentDto.CGPA,
                InstructorId = studentDto.InstructorId,
                PasswordHash = _passwordHasher.HashPassword(null, studentDto.Password)
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student registered successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _context.Students
                .Include(s => s.Instructor)
                .Select(s => new
                {
                    Name = $"{s.FirstName} {s.LastName}",
                    s.StudentId,
                    Action = "View"
                })
                .ToListAsync();

            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentProfile(int id)
        {
            var student = await _context.Students
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var profile = new
            {
                FullName = $"{student.FirstName} {student.LastName}",
                ID = student.StudentId,
                Major = "Computer Science",
                CGpa=student.CGPA,// You can add this to Student model if needed
                Contacts = new
                {
                    Phone = student.Phone,
                    Email = student.Email,
                    Advisor = student.Instructor?.FirstName + " " + student.Instructor?.LastName
                },
                Statistics = new
                {
                    Hours = "108/140",
                    AttendancePercentage = "90%"
                }
            };

            return Ok(profile);
        }

        [HttpPost("assign-schedule")]
        public async Task<IActionResult> AssignStudentSchedule([FromBody] StudentScheduleDto scheduleDto)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == scheduleDto.StudentId);
            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var courseCodes = scheduleDto.CourseSchedules.Select(c => c.CourseCode).Distinct();

            // Verify all courses exist
            var existingCourses = await _context.Courses
                .Where(c => courseCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToListAsync();

            var missingCourses = courseCodes.Except(existingCourses).ToList();
            if (missingCourses.Any())
            {
                return BadRequest(new
                {
                    message = "Some courses not found",
                    missingCourses = missingCourses
                });
            }

            // Remove existing schedules for this student
            var existingStudentSchedules = await _context.StudentSchedules
                .Where(ss => ss.StudentId == student.Id)
                .ToListAsync();

            _context.StudentSchedules.RemoveRange(existingStudentSchedules);

            // Add new schedules
            var studentSchedules = new List<StudentSchedule>();
            foreach (var courseSchedule in scheduleDto.CourseSchedules)
            {
                // Get all schedules for this course
                var schedules = await _context.Schedules
                    .Where(s => s.CourseCode == courseSchedule.CourseCode)
                    .ToListAsync();

                if (!schedules.Any())
                {
                    return BadRequest(new
                    {
                        message = $"No schedules found for course {courseSchedule.CourseCode}"
                    });
                }

                // Add either all sections or just lectures based on IncludeAllSections
                var schedulesToAdd = courseSchedule.IncludeAllSections
                    ? schedules
                    : schedules.Where(s => s.IsLecture).ToList();

                studentSchedules.AddRange(schedulesToAdd.Select(schedule => new StudentSchedule
                {
                    StudentId = student.Id,
                    ScheduleId = schedule.Id
                }));
            }

            await _context.StudentSchedules.AddRangeAsync(studentSchedules);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Student schedule updated successfully",
                assignedCourses = scheduleDto.CourseSchedules.Select(c => c.CourseCode)
            });
        }

        [Authorize(Roles = "Admin,Student")]
        [HttpGet("{studentId}/schedule")]
        public async Task<IActionResult> GetStudentSchedule(string studentId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var schedule = await _context.StudentSchedules
                .Include(ss => ss.Schedule)
                    .ThenInclude(s => s.Course)
                .Include(ss => ss.Schedule)
                    .ThenInclude(s => s.Instructor)
                .Where(ss => ss.StudentId == student.Id)
                .OrderBy(ss => ss.Schedule.DayOfWeek)
                .ThenBy(ss => ss.Schedule.StartTime)
                .Select(ss => new
                {
                    ss.Schedule.Id,
                    ss.Schedule.CourseCode,
                    CourseName = ss.Schedule.Course.Name,
                    Day = ss.Schedule.DayOfWeek.ToString(),
                    StartTime = ss.Schedule.StartTime.ToString(@"hh\:mm"),
                    EndTime = ss.Schedule.EndTime.ToString(@"hh\:mm"),
                    ss.Schedule.Location,
                    Type = ss.Schedule.IsLecture ? "Lecture" : "Section",
                    InstructorName = $"{ss.Schedule.Instructor.FirstName} {ss.Schedule.Instructor.LastName}"
                })
                .ToListAsync();

            return Ok(schedule);
        }
    }
}
