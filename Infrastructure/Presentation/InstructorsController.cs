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
    [Route("api/instructors")]
    [ApiController]
    public class InstructorsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Instructor> _passwordHasher;

        public InstructorsController(AppDbContext context, IPasswordHasher<Instructor> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
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
                FirstName= instructorDto.FirstName,
                LastName = instructorDto.LastName,
                Email = instructorDto.Email,
                Phone = instructorDto.Phone,
                Department = instructorDto.Department,
                OfficeAddress = instructorDto.OfficeAddress,
                PasswordHash = _passwordHasher.HashPassword(null, instructorDto.Password)
            };

            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Instructor registered successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInstructors()
        {
            var instructors = await _context.Instructors
                .Select(i => new
                {
                    i.InstructorId,
                    Name =$"{i.FirstName} {i.LastName}",
                    i.Email,
                    i.Phone,
                    i.Department
                })
                .ToListAsync();

            return Ok(instructors);
        }

        [HttpPost("assign-schedule")]
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
    }
}
