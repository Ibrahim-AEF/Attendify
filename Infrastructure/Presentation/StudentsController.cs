using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
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

        public StudentsController(AppDbContext context)
        {
            _context = context;
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
                StudentId = studentDto.StudentId,
                FirstName = studentDto.FirstName,
                LastName = studentDto.LastName,
                Email = studentDto.Email,
                Phone = studentDto.Phone,
                CGPA = studentDto.CGPA,
                InstructorId = studentDto.InstructorId
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
    }
}
