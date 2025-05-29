using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class StudentAttendanceDto
    {
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public List<LectureAttendanceDto> Attendances { get; set; }
    }
}
