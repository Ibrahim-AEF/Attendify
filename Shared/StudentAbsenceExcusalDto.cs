using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class StudentAbsenceExcusalDto
    {
        public int LectureId { get; set; }
        public string Reason { get; set; }
        public string FileUrl { get; set; } // For uploaded excusal documents
    }
}
