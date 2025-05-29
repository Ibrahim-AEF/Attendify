using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class InstructorScheduleDto
    {
        public string InstructorId { get; set; }
        public List<ScheduleDto> Schedules { get; set; }
    }
}
