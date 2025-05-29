using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Presistance.Data;
using Shared;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Presentation
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Admin> _userManager;
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Instructor> _passwordHasherI;
        private readonly IPasswordHasher<Student> _passwordHasherS;

        public AdminController(UserManager<Admin> userManager , AppDbContext context, IPasswordHasher<Instructor> passwordHasherI, IPasswordHasher<Student> passwordHasherS)
        {
            _userManager = userManager;
            _context = context;
            _passwordHasherI = passwordHasherI;
            _passwordHasherS = passwordHasherS;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Get ID from sub claim (not email)
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(adminId))
                return Unauthorized();

            var admin = await _userManager.FindByIdAsync(adminId); // Now using ID

            // Debugging logs
            Console.WriteLine($"Admin ID from token: {adminId}");
            Console.WriteLine($"Admin found: {admin != null}");

            return Ok(new { admin.FullName, admin.Email, admin.PhoneNumber });
        }

        [HttpPost("register-instructor")]
        public async Task<IActionResult> RegisterInstructor([FromBody] InstructorDto instructorDto)
        {
            if (await _context.Instructors.AnyAsync(i => i.InstructorId == instructorDto.InstructorId))
            {
                return BadRequest(new { message = "Instructor ID already exists" });
            }

            if (await _context.Instructors.AnyAsync(i => i.Email == instructorDto.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Create instructor record
            var instructor = new Instructor
            {
                //Id=instructorDto.Id,
                InstructorId = instructorDto.InstructorId,
                FirstName = instructorDto.FirstName,
                LastName = instructorDto.LastName,
                Email = instructorDto.Email,
                Phone = instructorDto.Phone,
                Department = instructorDto.Department,
                OfficeAddress = instructorDto.OfficeAddress,
                PasswordHash = _passwordHasherI.HashPassword(null, instructorDto.Password)
            };

            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Instructor registered successfully" });
        }

        [HttpGet("instructors")]
        public async Task<IActionResult> GetAllInstructors()
        {
            var instructors = await _context.Instructors
                .Select(i => new
                {
                    i.InstructorId,
                    Name = $"{i.FirstName} {i.LastName}",
                    i.Email,
                    i.Phone,
                    i.Department
                })
                .ToListAsync();

            return Ok(instructors);
        }

        [HttpPost("assign-instructor-schedule")]
        public async Task<IActionResult> AssignSchedule([FromBody] InstructorScheduleDto scheduleDto)
        {
            var instructor = await _context.Instructors.FindAsync(scheduleDto.InstructorId);
            if (instructor == null)
            {
                return NotFound(new { message = "Instructor not found" });
            }

            var courseCodes = scheduleDto.Schedules.Select(s => s.CourseCode).Distinct();
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

            var schedules = new List<Schedule>();
            foreach (var schedule in scheduleDto.Schedules)
            {
                if (!await _context.Courses.AnyAsync(c => c.Code == schedule.CourseCode))
                {
                    return BadRequest(new { message = $"Course with code {schedule.CourseCode} not found" });
                }

                var newSchedule = new Schedule
                {
                    CourseCode = schedule.CourseCode,
                    InstructorId = scheduleDto.InstructorId,
                    DayOfWeek = schedule.DayOfWeek,
                    StartTime = TimeSpan.Parse(schedule.StartTime),
                    EndTime = TimeSpan.Parse(schedule.EndTime),
                    Location = schedule.Location,
                    IsLecture = schedule.IsLecture
                };
                schedules.Add(newSchedule);
            }

            await _context.Schedules.AddRangeAsync(schedules);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Schedule assigned successfully" });
        }

        [HttpGet("{instructorId}/schedule")]
        public async Task<IActionResult> GetInstructorSchedule(string instructorId)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Course)
                .Where(s => s.InstructorId == instructorId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .Select(s => new
                {
                    s.Id,
                    s.CourseCode,
                    CourseName = s.Course.Name,
                    Day = s.DayOfWeek.ToString(),
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    s.Location,
                    Type = s.IsLecture ? "Lecture" : "Section"
                })
                .ToListAsync();

            return Ok(schedules);
        }

        [HttpPost("register-student")]
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
                Id = studentDto.Id,
                StudentId = studentDto.StudentId,
                FirstName = studentDto.FirstName,
                LastName = studentDto.LastName,
                Email = studentDto.Email,
                Phone = studentDto.Phone,
                CGPA = studentDto.CGPA,
                InstructorId = studentDto.InstructorId,
                PasswordHash = _passwordHasherS.HashPassword(null, studentDto.Password)
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student registered successfully" });
        }

        [HttpGet("students")]
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
        public async Task<IActionResult> GetStudentProfile(string id)
        {
            var student = await _context.Students
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var profile = new
            {
                FullName = $"{student.FirstName} {student.LastName}",
                ID = student.StudentId,
                Major = "Computer Science",
                CGpa = student.CGPA,// You can add this to Student model if needed
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

        [HttpPost("assign-schedule-student")]
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
        [HttpGet("{studentId}/studentschedule")]
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

        [HttpPost("create-course")]
        public async Task<IActionResult> CreateCourse([FromBody] CourseDto courseDto)
        {
            if (await _context.Courses.AnyAsync(c => c.Code == courseDto.Code))
            {
                return BadRequest(new { message = "Course code already exists" });
            }

            var instructor = await _context.Instructors.FindAsync(courseDto.InstructorId);
            if (instructor == null)
            {
                return BadRequest(new { message = "Instructor not found" });
            }

            var course = new Course
            {
                Code = courseDto.Code,
                Name = courseDto.Name,
                InstructorId = courseDto.InstructorId
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Course created successfully",
                course = new
                {
                    course.Code,
                    course.Name,
                    Instructor = $"{instructor.FirstName} {instructor.LastName}"
                }
            });
        }

        [HttpGet("get-courses")]
        public async Task<IActionResult> GetAllCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Instructor)
                .Select(c => new
                {
                    c.Code,
                    c.Name,
                    Instructor = $"{c.Instructor.FirstName} {c.Instructor.LastName}"
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpGet("attendance-reports")]
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

        [HttpGet("student-attendance-reports/{studentId}")]
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

        [HttpGet("course-attendance-reports/{studentId}/{courseCode}")]
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

        [HttpPost("send-alert")]
        public async Task<IActionResult> SendAlert([FromBody] AlertDto alertDto)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == alertDto.StudentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Validate course code if provided
            if (!string.IsNullOrEmpty(alertDto.CourseCode))
            {
                var courseExists = await _context.Courses
                    .AnyAsync(c => c.Code == alertDto.CourseCode);

                if (!courseExists)
                {
                    return BadRequest(new { message = "Invalid course code" });
                }
            }

            var alert = new Alert
            {
                StudentId = student.Id,
                Message = alertDto.Message,
                CourseCode = alertDto.CourseCode,
                SentDate = DateTime.Now,
                IsRead = false
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Alert sent successfully",
                alertId = alert.Id
            });
        }

        [HttpGet("student-alert/{studentId}")]
        public async Task<IActionResult> GetStudentAlerts(string studentId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var alerts = await _context.Alerts
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.SentDate)
                .Select(a => new
                {
                    a.Id,
                    a.Message,
                    a.CourseCode,
                    SentDate = a.SentDate.ToString("yyyy-MM-dd HH:mm"),
                    a.IsRead,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    StudentId = student.StudentId
                })
                .ToListAsync();

            return Ok(alerts);
        }

        [HttpPut("{alertId}/mark-as-read")]
        public async Task<IActionResult> MarkAlertAsRead(int alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert == null)
            {
                return NotFound(new { message = "Alert not found" });
            }

            alert.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Alert marked as read" });
        }

        [HttpDelete("{alertId}")]
        public async Task<IActionResult> DeleteAlert(int alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert == null)
            {
                return NotFound(new { message = "Alert not found" });
            }

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Alert deleted successfully" });
        }
    }
}
