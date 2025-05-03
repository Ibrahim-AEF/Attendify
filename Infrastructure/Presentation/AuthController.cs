using Attendify.Helpers;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(
            UserManager<Admin> userManager,
            SignInManager<Admin> signInManager,
            JwtHelper jwtHelper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtHelper = jwtHelper;
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
    }
}
