using Domain.Entities;
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
    [Route("api/courses")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
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

        [HttpGet]
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
    }

    public class CourseDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string InstructorId { get; set; }
    }
}
