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
    [Route("api/absence-excusals")]
    [ApiController]
    public class AbsenceExcusalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AbsenceExcusalController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAbsenceExcusals()
        {
            var excusals = await _context.AbsenceExcusals
                .Include(ae => ae.Student)
                .Include(ae => ae.Lecture)
                    .ThenInclude(l => l.Course)
                .Select(ae => new AbsenceExcusalDto
                {
                    Id = ae.Id,
                    StudentName = $"{ae.Student.FirstName} {ae.Student.LastName}",
                    StudentId = ae.Student.StudentId,
                    CourseCode = ae.Lecture.Course.Code,
                    LectureDate = ae.Lecture.Date.ToString("yyyy-MM-dd"),
                    Reason = ae.Reason,
                    Status = ae.IsApproved == null ? "Pending" :
                            (ae.IsApproved.Value ? "Approved" : "Rejected")
                })
                .ToListAsync();

            return Ok(excusals);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveAbsenceExcusal(int id)
        {
            return await UpdateExcusalStatus(id, true);
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectAbsenceExcusal(int id)
        {
            return await UpdateExcusalStatus(id, false);
        }

        private async Task<IActionResult> UpdateExcusalStatus(int id, bool isApproved)
        {
            var excusal = await _context.AbsenceExcusals
                .Include(ae => ae.Student)
                .Include(ae => ae.Lecture)
                .FirstOrDefaultAsync(ae => ae.Id == id);

            if (excusal == null)
            {
                return NotFound(new { message = "Absence excusal not found" });
            }

            if (excusal.IsApproved != null)
            {
                return BadRequest(new { message = "Excusal has already been processed" });
            }

            excusal.IsApproved = isApproved;
            excusal.ReviewDate = DateTime.Now;

            // If approved, update the attendance record
            if (isApproved)
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

            return Ok(new { message = $"Absence excusal {(isApproved ? "approved" : "rejected")}" });
        }
    }
}
