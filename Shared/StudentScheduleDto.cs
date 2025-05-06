using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class StudentScheduleDto
    {
        public string StudentId { get; set; }
        public List<int> ScheduleIds { get; set; } // IDs of the schedules to assign
    }
}
