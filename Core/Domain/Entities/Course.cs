using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Course
    {
        [Key]
        public string Code { get; set; } 

        [Required]
        public string Name { get; set; }

        [Required]
        public string InstructorId { get; set; }

        // Navigation properties
        public Instructor Instructor { get; set; }
        public ICollection<Lecture> Lectures { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
        public ICollection<Schedule> Schedules { get; set; }
    }
}
