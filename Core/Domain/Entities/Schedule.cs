using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CourseCode { get; set; }

        [Required]
        public string InstructorId { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public string Location { get; set; }

        public bool IsLecture { get; set; } // True for lecture, false for section

        // Navigation properties
        public Course Course { get; set; }
        public Instructor Instructor { get; set; }
    }
}
