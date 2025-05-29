using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class ScheduleDto
    {
        public string CourseCode { get; set; }
        public string InstructorId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public string StartTime { get; set; } // Format: "HH:mm"
        public string EndTime { get; set; }   // Format: "HH:mm"
        public string Location { get; set; }
        public bool IsLecture { get; set; }
    }
}
