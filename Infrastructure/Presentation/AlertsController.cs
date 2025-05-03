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
    [Route("api/alerts")]
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AlertsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("send")]
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

        [HttpGet("student/{studentId}")]
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
