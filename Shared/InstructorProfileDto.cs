using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class InstructorProfileDto
    {
        public string InstructorId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
        public string OfficeAddress { get; set; }
        public List<InstructorCourseDto> Courses { get; set; }
    }
}
