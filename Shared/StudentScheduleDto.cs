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
        public List<StudentCourseScheduleDto> CourseSchedules { get; set; }
    }
    public class StudentCourseScheduleDto
    {
        public string CourseCode { get; set; }
        public bool IncludeAllSections { get; set; } = true;
    }
}
