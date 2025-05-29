using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class LectureAttendanceDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } // "Present", "Absent", "Excused"
        public string Time { get; set; }
    }
}
