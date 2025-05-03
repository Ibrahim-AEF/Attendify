using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class AbsenceExcusalDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string StudentId { get; set; }
        public string CourseCode { get; set; }
        public string LectureDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; } // "Pending", "Approved", "Rejected"
    }
}
