using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public AdminController(UserManager<Admin> userManager)
        {
            _userManager = userManager;
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
    }
}
