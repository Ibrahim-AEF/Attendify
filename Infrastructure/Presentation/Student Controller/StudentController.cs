using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Presistance.Data;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Student_Controller
{
    [Authorize(Roles = "Student")]
    [Route("api/student")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public StudentController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Get student profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .Include(s => s.Instructor)
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Calculate attendance statistics (simplified for demo)
            var totalClasses = await _context.Attendances
                .CountAsync(a => a.StudentId == student.Id);
            var presentClasses = await _context.Attendances
                .CountAsync(a => a.StudentId == student.Id && a.IsPresent);

            return Ok(new
            {
                FullName = $"{student.FirstName} {student.LastName}",
                StudentId = student.StudentId,
                Major = "Computer Science", // Could be stored in Student model
                CGPA = student.CGPA,
                Rank = 68, // Could be calculated
                Class = 7, // Could be stored in Student model
                Statistics = new
                {
                    Hours = "108/140", // Could be calculated
                    AttendancePercentage = totalClasses > 0 ?
                        $"{Math.Round((presentClasses / (double)totalClasses) * 100)}%" : "0%",
                    CreditEarned = "90%" // Could be calculated
                },
                Contacts = new
                {
                    Phone = student.Phone,
                    Email = student.Email,
                    Advisor = $"{student.Instructor?.FirstName} {student.Instructor?.LastName}"
                }
            });
        }

        // Get student schedule
        [HttpGet("schedule")]
        public async Task<IActionResult> GetSchedule()
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

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
                    Day = ss.Schedule.DayOfWeek.ToString(),
                    Time = $"{ss.Schedule.StartTime:hh\\:mm} - {ss.Schedule.EndTime:hh\\:mm}",
                    CourseName = ss.Schedule.Course.Name,
                    Location = ss.Schedule.Location,
                    Type = ss.Schedule.IsLecture ? "LEC" : "SEC",
                    Instructor = $"{ss.Schedule.Instructor.FirstName} {ss.Schedule.Instructor.LastName}"
                })
                .ToListAsync();

            return Ok(schedule);
        }

        // Get today's classes
        [HttpGet("today-classes")]
        public async Task<IActionResult> GetTodayClasses()
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var today = DateTime.Today.DayOfWeek;
            var schedules = await _context.StudentSchedules
                .Include(ss => ss.Schedule)
                    .ThenInclude(s => s.Course)
                .Where(ss => ss.StudentId == student.Id && ss.Schedule.DayOfWeek == today)
                .OrderBy(ss => ss.Schedule.StartTime)
                .Select(ss => new
                {
                    CourseName = ss.Schedule.Course.Name,
                    Time = $"{ss.Schedule.StartTime:hh\\:mm} - {ss.Schedule.EndTime:hh\\:mm}",
                    Location = ss.Schedule.Location,
                    Type = ss.Schedule.IsLecture ? "LEC" : "SEC"
                })
                .ToListAsync();

            return Ok(schedules);
        }

        // Get enrolled courses
        [HttpGet("courses")]
        public async Task<IActionResult> GetEnrolledCourses()
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var courses = await _context.StudentSchedules
                .Include(ss => ss.Schedule)
                    .ThenInclude(s => s.Course)
                .Where(ss => ss.StudentId == student.Id)
                .Select(ss => ss.Schedule.Course)
                .Distinct()
                .Select(c => new
                {
                    c.Code,
                    c.Name,
                    Action = "Details"
                })
                .ToListAsync();

            return Ok(courses);
        }

        // Get course attendance
        [HttpGet("courses/{courseCode}/attendance")]
        public async Task<IActionResult> GetCourseAttendance(string courseCode)
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Verify student is enrolled in this course
            var isEnrolled = await _context.StudentSchedules
                .AnyAsync(ss => ss.StudentId == student.Id && ss.Schedule.CourseCode == courseCode);

            if (!isEnrolled)
            {
                return BadRequest(new { message = "You are not enrolled in this course" });
            }

            var lectures = await _context.Lectures
                .Where(l => l.CourseCode == courseCode)
                .OrderByDescending(l => l.Date)
                .Take(6) // Get last 6 lectures as shown in UI
                .OrderBy(l => l.Date)
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.StudentId == student.Id &&
                           lectures.Select(l => l.Id).Contains(a.LectureId))
                .ToListAsync();

            var attendanceDetails = lectures.Select(l => new
            {
                LectureName = $"{l.LectureName}",
                Date = l.Date.ToString("dd-MM-yyyy"),
                Status = attendances.FirstOrDefault(a => a.LectureId == l.Id)?.IsPresent == true ?
                         "Present" : "Absent"
            }).ToList();

            return Ok(new
            {
                CourseCode = courseCode,
                Lectures = attendanceDetails
            });
        }

        // Get attendance report for a course
        [HttpGet("courses/{courseCode}/report")]
        public async Task<IActionResult> GetAttendanceReport(string courseCode)
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Verify student is enrolled in this course
            var isEnrolled = await _context.StudentSchedules
                .AnyAsync(ss => ss.StudentId == student.Id && ss.Schedule.CourseCode == courseCode);

            if (!isEnrolled)
            {
                return BadRequest(new { message = "You are not enrolled in this course" });
            }

            var attendanceRecords = await _context.Attendances
                .Include(a => a.Lecture)
                .Where(a => a.StudentId == student.Id && a.Lecture.CourseCode == courseCode)
                .ToListAsync();

            var totalClasses = attendanceRecords.Count;
            var presentDays = attendanceRecords.Count(a => a.IsPresent);
            var absentDays = attendanceRecords.Count(a => !a.IsPresent && !a.IsExcused);
            var excusedDays = attendanceRecords.Count(a => a.IsExcused);
            var attendancePercentage = totalClasses > 0 ?
                (presentDays / (double)totalClasses) * 100 : 0;

            return Ok(new
            {
                CourseCode = courseCode,
                Hours = "3 HRs",
                AttendancePercentage = $"{attendancePercentage:F1}%",
                Stats = new
                {
                    TotalClasses = totalClasses,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    ExcusedDays = excusedDays
                },
                Message = attendancePercentage >= 80 ? "Keep Up the Good Job!" :
                         "You need to attend more classes!"
            });
        }

        // Get notifications
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var today = DateTime.Today;
            var last7Days = today.AddDays(-7);

            // Get all alerts with their related course (if exists)
            var alerts = await _context.Alerts
                .Include(a => a.Student)
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.SentDate)
                .ToListAsync();

            // Prepare response lists
            var todayAlerts = new List<object>();
            var last7DaysAlerts = new List<object>();

            foreach (var alert in alerts)
            {
                string senderName;
                string senderEmail;

                // Determine sender type based on CourseCode presence
                if (string.IsNullOrEmpty(alert.CourseCode))
                {
                    // Admin notification
                    senderName = "Ahmed [Admin]";
                    senderEmail = "Ahmed123@must.edu.eg";
                }
                else
                {
                    // Instructor notification - get from course
                    var instructor = await _context.Courses
                        .Where(c => c.Code == alert.CourseCode)
                        .Select(c => new {
                            Name = $"{c.Instructor.FirstName} {c.Instructor.LastName}",
                            c.Instructor.Email
                        })
                        .FirstOrDefaultAsync();

                    senderName = instructor?.Name ?? "Instructor";
                    senderEmail = instructor?.Email ?? "instructor@must.edu.eg";
                }

                var alertData = new
                {
                    //alert.Id,
                    Title = senderName,
                    Time = alert.SentDate.ToString("h:mm tt"),
                    Date = alert.SentDate.ToString("dd/MM/yyyy"),
                    Email = senderEmail,
                    Message = alert.Message,
                    //alert.IsRead
                };

                if (alert.SentDate.Date == today.Date)
                {
                    todayAlerts.Add(alertData);
                }
                else if (alert.SentDate.Date >= last7Days.Date && alert.SentDate.Date < today.Date)
                {
                    last7DaysAlerts.Add(alertData);
                }
            }

            // Mark as read
            var unreadAlerts = alerts.Where(a => !a.IsRead).ToList();
            if (unreadAlerts.Any())
            {
                unreadAlerts.ForEach(a => a.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                Today = todayAlerts,
                Last7Days = last7DaysAlerts
            });
        }

        // Submit absence excusal
        [HttpPost("absence-excusals")]
        public async Task<IActionResult> SubmitAbsenceExcusal([FromBody] StudentAbsenceExcusalDto excusalDto)
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Verify the lecture exists and belongs to a course the student is enrolled in
            var lecture = await _context.Lectures
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == excusalDto.LectureId);

            if (lecture == null)
            {
                return NotFound(new { message = "Lecture not found" });
            }

            var isEnrolled = await _context.StudentSchedules
                .AnyAsync(ss => ss.StudentId == student.Id &&
                               ss.Schedule.CourseCode == lecture.CourseCode);

            if (!isEnrolled)
            {
                return BadRequest(new { message = "You are not enrolled in this course" });
            }

            // Check if excusal already exists
            var existingExcusal = await _context.AbsenceExcusals
                .FirstOrDefaultAsync(ae => ae.StudentId == student.Id &&
                                         ae.LectureId == excusalDto.LectureId);

            if (existingExcusal != null)
            {
                return BadRequest(new { message = "Excusal already submitted for this lecture" });
            }

            // Create new excusal
            var excusal = new AbsenceExcusal
            {
                StudentId = student.Id,
                LectureId = excusalDto.LectureId,
                Reason = excusalDto.Reason,
                RequestDate = DateTime.Now
            };

            _context.AbsenceExcusals.Add(excusal);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Absence excusal submitted successfully" });
        }

        // Scan QR code and record attendance
        [HttpPost("scan-attendance")]
        public async Task<IActionResult> ScanAttendance([FromBody] ScanAttendanceDto scanDto)
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id.ToString() == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Parse QR code data (format: CourseCode|LectureDate|LectureName|Timestamp)
            var qrDataParts = scanDto.QRCodeData.Split('|');
            if (qrDataParts.Length != 4)
            {
                return BadRequest(new { message = "Invalid QR code format" });
            }

            var courseCode = qrDataParts[0];
            var lectureDate = DateTime.ParseExact(qrDataParts[1], "yyyyMMdd", null);
            var lectureName = qrDataParts[2];

            // Verify the lecture exists and is active
            var lecture = await _context.Lectures
                .FirstOrDefaultAsync(l => l.CourseCode == courseCode &&
                                         l.Date.Date == lectureDate.Date &&
                                         l.LectureName == lectureName);

            if (lecture == null)
            {
                return NotFound(new { message = "Lecture not found" });
            }

            if (lecture.QRCodeExpiry < DateTime.UtcNow)
            {
                return BadRequest(new { message = "QR code has expired" });
            }

            // Verify student is enrolled in this course
            var isEnrolled = await _context.StudentSchedules
                .AnyAsync(ss => ss.StudentId == student.Id &&
                               ss.Schedule.CourseCode == courseCode);

            if (!isEnrolled)
            {
                return BadRequest(new { message = "You are not enrolled in this course" });
            }

            // Check if attendance already recorded
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == student.Id &&
                                         a.LectureId == lecture.Id);

            if (existingAttendance != null)
            {
                return BadRequest(new { message = "Attendance already recorded for this lecture" });
            }

            // Here you would verify the face recognition result
            // For demo, we'll assume it was successful
            if (!scanDto.FaceVerified)
            {
                return BadRequest(new { message = "Face verification failed" });
            }

            // Record attendance
            var attendance = new Attendance
            {
                StudentId = student.Id,
                LectureId = lecture.Id,
                AttendanceTime = DateTime.Now,
                IsPresent = true
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Get course and instructor details for response
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Code == courseCode);

            return Ok(new
            {
                Message = "The Attendance Taken Successfully!!!",
                StudentName = $"{student.FirstName} {student.LastName}",
                StudentId = student.StudentId,
                Major = "Computer Science", // Could be from Student model
                CourseCode = courseCode,
                CourseName = course?.Name,
                Time = $"{lecture.Date.ToString("h:mm tt")} to {lecture.Date.AddHours(2).ToString("h:mm tt")}",
                Day = lecture.Date.ToString("ddd"),
                Instructor = course != null ?
                    $"DR. {course.Instructor?.FirstName} {course.Instructor?.LastName}" : "N/A",
                Session = $"session #{new Random().Next(1000, 9999)}"
            });
        }
    }
}
