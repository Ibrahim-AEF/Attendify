using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presistance.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation
{
    [Authorize(Roles = "Admin")]
    [Route("api/attendance")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetAttendanceReports()
        {
            var students = await _context.Students
                .Select(s => new
                {
                    Name = $"{s.FirstName} {s.LastName}",
                    s.StudentId,
                    Action = "View"
                })
                .ToListAsync();

            return Ok(students);
        }

        [HttpGet("reports/{studentId}")]
        public async Task<IActionResult> GetStudentAttendanceReport(string studentId)
        {

            var student = await _context.Students
                .Include(s => s.StudentSchedules)
                    .ThenInclude(ss => ss.Schedule)
                        .ThenInclude(s => s.Course)
                .Include(s => s.Attendances)
                    .ThenInclude(a => a.Lecture)
                        .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var registeredCourses = student.StudentSchedules
                .Select(ss => ss.Schedule.Course)
                .Distinct()
                .ToList();

            var coursesWithAttendance = registeredCourses.Select(course =>
            {
                var courseAttendances = student.Attendances
                    .Where(a => a.Lecture.Course.Code == course.Code)
                    .ToList();

                var totalClasses = courseAttendances.Count;
                var presentDays = courseAttendances.Count(a => a.IsPresent);
                var attendancePercentage = totalClasses > 0 ?
                    (presentDays / (float)totalClasses) * 100 : 0;

                return new
                {
                    Subject = course.Code,
                    CourseName = course.Name,
                    Action = "Report",
                    //TotalClasses = totalClasses,
                    //PresentDays = presentDays,
                    //AttendancePercentage = $"{attendancePercentage:F1}%"
                };
            }).ToList();

            return Ok(new
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                StudentId = student.StudentId,
                Courses = coursesWithAttendance
            });
        }

        [HttpGet("reports/{studentId}/{courseCode}")]
        public async Task<IActionResult> GetCourseAttendanceReport(string studentId, string courseCode)
        {
            var attendanceRecords = await _context.Attendances
                .Include(a => a.Lecture)
                    .ThenInclude(l => l.Course)
                .Where(a => a.Student.StudentId == studentId && a.Lecture.Course.Code == courseCode)
                .OrderBy(a => a.Lecture.Date)
                .ToListAsync();

            if (!attendanceRecords.Any())
            {
                return NotFound(new { message = "No attendance records found" });
            }

            var totalClasses = attendanceRecords.Count;
            var presentDays = attendanceRecords.Count(a => a.IsPresent);
            var absentDays = attendanceRecords.Count(a => !a.IsPresent && !a.IsExcused);
            var excusedDays = attendanceRecords.Count(a => a.IsExcused);
            var attendancePercentage = (presentDays / (float)totalClasses) * 100;

            return Ok(new
            {
                CourseCode = courseCode,
                TotalClasses = totalClasses,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                ExcusedDays = excusedDays,
                AttendancePercentage = $"{attendancePercentage:F1}%",
                StudentId = studentId,
                StudentName = $"{attendanceRecords.First().Student.FirstName} {attendanceRecords.First().Student.LastName}"
            });
        }
    }
}
