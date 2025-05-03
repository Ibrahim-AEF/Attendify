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
        private readonly UserManager<Admin> _userManager;

        public InstructorsController(AppDbContext context, UserManager<Admin> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            // Create a user account for the instructor
            var user = new Admin
            {
                UserName = instructorDto.Email,
                Email = instructorDto.Email,
                FullName = $"{instructorDto.FirstName} {instructorDto.LastName}",
                PhoneNumber = instructorDto.Phone
            };

            var result = await _userManager.CreateAsync(user, instructorDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to create user account", errors = result.Errors });
            }

            // Add to Instructor role
            await _userManager.AddToRoleAsync(user, "Instructor");

            // Create instructor record
            var instructor = new Instructor
            {
                Id = user.Id,
                InstructorId = instructorDto.InstructorId,
                FirstName= instructorDto.FirstName,
                LastName = instructorDto.LastName,
                Email = instructorDto.Email,
                Phone = instructorDto.Phone,
                Department = instructorDto.Department,
                OfficeAddress = instructorDto.OfficeAddress
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
                    i.Id,
                    Name =$"{i.FirstName} {i.LastName}",
                    i.Email,
                    i.Phone,
                    i.Department
                })
                .ToListAsync();

            return Ok(instructors);
        }
    }
}
