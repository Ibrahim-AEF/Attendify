using Attendify.Helpers;
using Domain.Entities;
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
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Admin> _userManager;
        private readonly SignInManager<Admin> _signInManager;
        private readonly JwtHelper _jwtHelper;
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Instructor> _instructorPasswordHasher;
        private readonly IPasswordHasher<Student> _studentPasswordHasher;

        public AuthController(
            UserManager<Admin> userManager,
            SignInManager<Admin> signInManager,
            JwtHelper jwtHelper ,
            AppDbContext context,
            IPasswordHasher<Instructor> instructorPasswordHasher,
            IPasswordHasher<Student> studentPasswordHasher)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtHelper = jwtHelper;
            _context = context;
            _instructorPasswordHasher = instructorPasswordHasher;
            _studentPasswordHasher = studentPasswordHasher;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var admin = await _userManager.FindByEmailAsync(loginDto.Email);
            if (admin == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(admin, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = _jwtHelper.GenerateJwtToken(admin);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Email = admin.Email,
                FullName = admin.FullName,
                PhoneNumber = admin.PhoneNumber,
                Role = "Admin"
            });
        }

        [HttpPost("instructor-login")]
        public async Task<IActionResult> InstructorLogin([FromBody] LoginDto loginDto)
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Email == loginDto.Email);
            if (instructor == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = _instructorPasswordHasher.VerifyHashedPassword(null, instructor.PasswordHash, loginDto.Password);
            if (result != PasswordVerificationResult.Success)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = _jwtHelper.GenerateJwtToken(instructor.InstructorId, instructor.Email, $"{instructor.FirstName} {instructor.LastName}", "Instructor");

            return Ok(new LoginResponseDto
            {
                Token = token,
                Email = instructor.Email,
                FullName = $"{instructor.FirstName} {instructor.LastName}",
                PhoneNumber = instructor.Phone,
                Role = "Instructor"
            });
        }

        [HttpPost("student-login")]
        public async Task<IActionResult> StudentLogin([FromBody] LoginDto loginDto)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == loginDto.Email);
            if (student == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = _studentPasswordHasher.VerifyHashedPassword(null, student.PasswordHash, loginDto.Password);
            if (result != PasswordVerificationResult.Success)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = _jwtHelper.GenerateJwtToken(student.Id.ToString(), student.Email, $"{student.FirstName} {student.LastName}", "Student");

            return Ok(new LoginResponseDto
            {
                Token = token,
                Email = student.Email,
                FullName = $"{student.FirstName} {student.LastName}",
                PhoneNumber = student.Phone,
                Role = "Student"
            });
        }
    }
}
