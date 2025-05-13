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

namespace Presentation.Instructor_Controller
{
    [Authorize(Roles = "Instructor")]
    [Route("api/instructor")]
    [ApiController]
    public class InstructorController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public InstructorController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Get instructor profile with courses
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            var instructor = await _context.Instructors
                .Include(i => i.Courses)
                .FirstOrDefaultAsync(i => i.InstructorId == instructorId);

            if (instructor == null)
            {
                return NotFound(new { message = "Instructor not found" });
            }

            return Ok(new
            {
                FullName = $"Dr. {instructor.FirstName} {instructor.LastName}",
                Faculty = "Faculty of Information technology",
                AssignedClasses = instructor.Courses.Select(c => c.Code).ToList(),
                Contacts = new
                {
                    Phone = instructor.Phone,
                    Email = instructor.Email,
                    OfficeAddress = "SS, first floor, Room 2"
                }
            });
        }

        // Get instructor schedule
        [HttpGet("schedule")]
        public async Task<IActionResult> GetSchedule()
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            var today = DateTime.Today.DayOfWeek.ToString().Substring(0, 3).ToUpper();

            var schedules = await _context.Schedules
               .Include(s => s.Course)
               .Where(s => s.InstructorId == instructorId)
               .OrderBy(s => s.DayOfWeek)
               .ThenBy(s => s.StartTime)
               .ToListAsync();

            var groupedSchedules = schedules
                .GroupBy(s => s.DayOfWeek.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => new
                    {
                        Time = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}",
                        CourseName = s.Course.Name,
                        Location = s.Location,
                        Type = s.IsLecture ? "LEC" : "SEC"
                    }).ToList()
                );

            return Ok(new
            {
                Today = today,
                Schedules = groupedSchedules
            });
        }

        // Get today's classes
        [HttpGet("today-classes")]
        public async Task<IActionResult> GetTodayClasses()
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            var today = DateTime.Today.DayOfWeek;
            var schedules = await _context.Schedules
                .Include(s => s.Course)
                .Where(s => s.InstructorId == instructorId && s.DayOfWeek == today)
                .OrderBy(s => s.StartTime)
                .Select(s => new
                {
                    CourseName = s.Course.Name,
                    Location = s.Location,
                    Time = $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
                })
                .ToListAsync();

            return Ok(schedules);
        }

        // Get courses for QR generation
        [HttpGet("courses-for-qr")]
        public async Task<IActionResult> GetCoursesForQrGeneration()
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            var courses = await _context.Courses
                .Where(c => c.InstructorId == instructorId)
                .Select(c => new
                {
                    c.Code,
                    Lectures = _context.Lectures
                        .Where(l => l.CourseCode == c.Code)
                        .Select(l => new
                        {
                            Id = l.Id,
                            Name = $"{l.LectureName}"
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(courses);
        }


        // Get course details
        [HttpGet("courses/{courseCode}")]
        public async Task<IActionResult> GetCourseDetails(string courseCode)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            var course = await _context.Courses
                .Include(c => c.Schedules)
                .Include(c => c.Lectures)
                .FirstOrDefaultAsync(c => c.Code == courseCode && c.InstructorId == instructorId);

            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            var lectures = course.Lectures
                .OrderBy(l => l.Date)
                .Select(l => new
                {
                    l.Id,
                    l.Date,
                    l.QRCode
                });

            var schedules = course.Schedules
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .Select(s => new
                {
                    s.Id,
                    Day = s.DayOfWeek.ToString(),
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    s.Location,
                    Type = s.IsLecture ? "Lecture" : "Section"
                });

            return Ok(new
            {
                course.Code,
                course.Name,
                Lectures = lectures,
                Schedules = schedules
            });
        }

        // Generate QR code for a lecture
        [HttpPost("generate-qr")]
        public async Task<IActionResult> GenerateQrCode([FromBody] QrCodeGenerationDto qrCodeDto)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            // Verify the course belongs to this instructor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == qrCodeDto.CourseCode && c.InstructorId == instructorId);

            if (course == null)
            {
                return BadRequest(new { message = "Course not found or not assigned to you" });
            }

            // Create or update lecture
            var lecture = await _context.Lectures
                .FirstOrDefaultAsync(l => l.CourseCode == qrCodeDto.CourseCode &&
                                        l.LectureName == qrCodeDto.LectureName);

            if (lecture == null)
            {
                lecture = new Lecture
                {
                    CourseCode = qrCodeDto.CourseCode,
                    Date = qrCodeDto.LectureDate,
                    LectureName = qrCodeDto.LectureName
                };
                _context.Lectures.Add(lecture);
            }

            // Generate QR code (in a real app, you'd use a QR code generation library)
            var qrCodeData = $"{course.Code}|{lecture.Date:yyyyMMdd}|{lecture.LectureName}|{DateTime.UtcNow.Ticks}";

            lecture.QRCode = qrCodeData;
            lecture.QRCodeExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                CourseCode = course.Code,
                Lecture = $"{lecture.LectureName}",
                QRCode = qrCodeData,
                ShareLink = $"{_configuration["BaseUrl"]}/qr/{qrCodeData}"
            });
        }

        // Get enrolled students for a course
        [HttpGet("courses/{courseCode}/students")]
        public async Task<IActionResult> GetEnrolledStudents(string courseCode)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            // Verify the course belongs to this instructor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == courseCode && c.InstructorId == instructorId);

            if (course == null)
            {
                return BadRequest(new { message = "Course not found or not assigned to you" });
            }

            var students = await _context.StudentSchedules
                .Include(ss => ss.Student)
                .Include(ss => ss.Schedule)
                .Where(ss => ss.Schedule.CourseCode == courseCode)
                .Select(ss => new
                {
                    FullName = $"{ss.Student.FirstName} {ss.Student.LastName}",
                    StudentId = ss.Student.StudentId,
                    HasDetails = true // Flag to show "View" button in UI
                })
                .Distinct()
                .ToListAsync();

            //return Ok(students);
            return Ok(new
            {
                CourseCode = courseCode,
                Students = students
            });
        }

        // Get individual student attendance - new endpoint to match UI
        // Get individual student attendance details for a course
        [HttpGet("courses/{courseCode}/students/{studentId}")]
        public async Task<IActionResult> GetStudentAttendanceDetails(string courseCode, string studentId)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new { message = "Authorization required" });
            }

            // Verify the course belongs to this instructor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == courseCode && c.InstructorId == instructorId);

            if (course == null)
            {
                return NotFound(new { message = "Course not found or not assigned to you" });
            }

            // Get the student
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Get the 6 most recent lectures for this course
            var lectures = await _context.Lectures
                .Where(l => l.CourseCode == courseCode)
                .OrderByDescending(l => l.Date)
                .Take(6)
                .OrderBy(l => l.Date) // Re-order chronologically for display
                .Select(l => new
                {
                    l.Id,
                    LectureName = $"Lec{l.LectureName}",
                    l.Date
                })
                .ToListAsync();

            // Get attendance records for this student
            var attendances = await _context.Attendances
                .Where(a => a.StudentId == student.Id &&
                           lectures.Select(l => l.Id).Contains(a.LectureId))
                .Select(a => new
                {
                    a.LectureId,
                    a.IsPresent,
                    a.IsExcused
                })
                .ToListAsync();

            // Format the response to match UI exactly
            var attendanceDetails = lectures.Select(l =>
            {
                var attendance = attendances.FirstOrDefault(a => a.LectureId == l.Id);
                return new
                {
                    l.LectureName,
                    Date = l.Date.ToString("dd-MM-yyyy"),
                    Status = attendance == null ? "Absent" :
                            attendance.IsPresent ? "Present" :
                            attendance.IsExcused ? "Excused" : "Absent"
                };
            }).ToList();

            return Ok(new
            {
                Student = new
                {
                    FullName = $"{student.FirstName} {student.LastName}",
                    student.StudentId
                },
                Attendances = attendanceDetails,
                Course = new
                {
                    course.Code,
                    course.Name
                }
            });
        }

        // Get student attendance for a course
        [HttpGet("courses/{courseCode}/attendance")]
        public async Task<IActionResult> GetCourseAttendance(string courseCode)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            // Verify the course belongs to this instructor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == courseCode && c.InstructorId == instructorId);

            if (course == null)
            {
                return BadRequest(new { message = "Course not found or not assigned to you" });
            }

            var lectureIds = await _context.Lectures
            .Where(l => l.CourseCode == courseCode)
            .OrderByDescending(l => l.Date)
            .Take(6)
            .Select(l => l.Id)
            .ToListAsync();

            // Get all students enrolled in the course
            var students = await _context.StudentSchedules
                .Include(ss => ss.Student)
                .Where(ss => ss.Schedule.CourseCode == courseCode)
                .Select(ss => ss.Student)
                .Distinct()
                .ToListAsync();

            // Get all attendance records for these students and lectures
            var attendances = await _context.Attendances
                .Where(a => lectureIds.Contains(a.LectureId) &&
                           students.Select(s => s.Id).Contains(a.StudentId))
                .ToListAsync();

            var result = students.Select(s => new
            {
                FullName = $"{s.FirstName} {s.LastName}",
                StudentId = s.StudentId,
                Attendances = lectureIds.Select(lectureId =>
                {
                    var attendance = attendances.FirstOrDefault(a =>
                        a.LectureId == lectureId && a.StudentId == s.Id);
                    return new
                    {
                        Status = attendance?.IsPresent == true ? "Present" : "Absent"
                    };
                }).ToList()
            }).ToList();

            return Ok(new
            {
                Students = result,
                Lectures = await _context.Lectures
                    .Where(l => lectureIds.Contains(l.Id))
                    .OrderBy(l => l.Date)
                    .Select(l => $"Lec{l.LectureName} {l.Date:dd-MM-yyyy}")
                    .ToListAsync()
            });

        }

        // Get attendance report
        [HttpGet("attendance-report")]
        public async Task<IActionResult> GetAttendanceReport(string courseCode)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            // Verify the course belongs to this instructor if specified
            if (!string.IsNullOrEmpty(courseCode))
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Code == courseCode && c.InstructorId == instructorId);

                if (course == null)
                {
                    return BadRequest(new { message = "Course not found or not assigned to you" });
                }
            }

            var courses = await _context.Courses
                .Where(c => c.InstructorId == instructorId)
                .Select(c => c.Code)
                .ToListAsync();

            return Ok(new
            {
                Courses = courses,
                // Simplified report data for UI
                ReportData = new List<object>()
            });
        }

        // Send notification to students
        [HttpPost("send-notification")]
        public async Task<IActionResult> SendNotification([FromBody] NotificationDto notificationDto)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            // Validate course if specified
            if (!string.IsNullOrEmpty(notificationDto.CourseCode))
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Code == notificationDto.CourseCode && c.InstructorId == instructorId);

                if (course == null)
                {
                    return BadRequest(new { message = "Course not found or not assigned to you" });
                }
            }

            // Get students to notify
            var studentsQuery = _context.Students.AsQueryable();

            if (!string.IsNullOrEmpty(notificationDto.CourseCode))
            {
                studentsQuery = studentsQuery
                    .Where(s => s.StudentSchedules.Any(ss => ss.Schedule.CourseCode == notificationDto.CourseCode));
            }
            else
            {
                // If no course specified, notify all students assigned to any of instructor's courses
                studentsQuery = studentsQuery
                    .Where(s => s.StudentSchedules.Any(ss =>
                        _context.Schedules.Any(sc =>
                            sc.Id == ss.ScheduleId &&
                            sc.InstructorId == instructorId)));
            }

            var students = await studentsQuery.ToListAsync();

            // Create alerts
            var alerts = students.Select(s => new Alert
            {
                StudentId = s.Id,
                Message = $"{notificationDto.Title}: {notificationDto.Message}",
                CourseCode = notificationDto.CourseCode,
                SentDate = DateTime.Now,
                IsRead = false
            }).ToList();

            await _context.Alerts.AddRangeAsync(alerts);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Notification sent successfully",
                recipientsCount = alerts.Count
            });
        }

        // Create quiz
        [HttpPost("create-quiz")]
        public async Task<IActionResult> CreateQuiz([FromBody] QuizDto quizDto)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            // Verify the course belongs to this instructor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == quizDto.CourseCode && c.InstructorId == instructorId);

            if (course == null)
            {
                return BadRequest(new { message = "Course not found or not assigned to you" });
            }

            // In a real implementation, you would save the quiz to database
            // This is simplified for demonstration
            var quiz = new
            {
                quizDto.Title,
                quizDto.CourseCode,
                quizDto.DurationMinutes,
                quizDto.PassMark,
                Questions = quizDto.Questions.Select(q => new
                {
                    q.QuestionText,
                    q.Options,
                    CorrectAnswer = q.Options[q.CorrectAnswerIndex]
                })
            };

            return Ok(new
            {
                message = "Quiz created successfully",
                quiz
            });
        }

        // Review absence excusals
        [HttpGet("absence-excusals")]
        public async Task<IActionResult> GetAbsenceExcusals()
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            var excusals = await _context.AbsenceExcusals
                .Include(ae => ae.Student)
                .Include(ae => ae.Lecture)
                    .ThenInclude(l => l.Course)
                .Where(ae => ae.Lecture.Course.InstructorId == instructorId && ae.IsApproved == null)
                .OrderBy(ae => ae.RequestDate)
                .Select(ae => new AbsenceExcusalDto
                {
                    Id = ae.Id,
                    StudentName = $"{ae.Student.FirstName} {ae.Student.LastName}",
                    StudentId = ae.Student.StudentId,
                    CourseCode = ae.Lecture.Course.Code,
                    LectureDate = ae.Lecture.Date.ToString("yyyy-MM-dd"),
                    Reason = ae.Reason,
                    Status = "Pending"
                })
                .ToListAsync();

            return Ok(excusals);
        }

        // Approve/reject absence excusal
        [HttpPut("absence-excusals/{id}/{action}")]
        public async Task<IActionResult> ProcessAbsenceExcusal(int id, string action)
        {
            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized();
            }

            if (action != "approve" && action != "reject")
            {
                return BadRequest(new { message = "Action must be either 'approve' or 'reject'" });
            }

            var excusal = await _context.AbsenceExcusals
                .Include(ae => ae.Student)
                .Include(ae => ae.Lecture)
                    .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(ae => ae.Id == id && ae.Lecture.Course.InstructorId == instructorId);

            if (excusal == null)
            {
                return NotFound(new { message = "Absence excusal not found" });
            }

            if (excusal.IsApproved != null)
            {
                return BadRequest(new { message = "Excusal has already been processed" });
            }

            excusal.IsApproved = action == "approve";
            excusal.ReviewDate = DateTime.Now;

            // If approved, update the attendance record
            if (excusal.IsApproved == true)
            {
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == excusal.StudentId &&
                                            a.LectureId == excusal.LectureId);

                if (attendance != null)
                {
                    attendance.IsExcused = true;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Absence excusal {action}d" });
        }
    }

}
