using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; } // Foreign key to Student

        [Required]
        public int LectureId { get; set; } // Foreign key to Lecture

        [Required]
        public DateTime AttendanceTime { get; set; }

        public bool IsPresent { get; set; }
        public bool IsExcused { get; set; }

        // Navigation properties
        public Student Student { get; set; }
        public Lecture Lecture { get; set; }
    }
}
