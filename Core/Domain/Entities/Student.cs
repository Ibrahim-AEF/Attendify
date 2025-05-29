using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } // University ID like 200010279

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        public float CGPA { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Added password field

        [Required]
        public string InstructorId { get; set; } // Foreign key to Instructor

        // Navigation properties
        public Instructor Instructor { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public ICollection<AbsenceExcusal> AbsenceExcusals { get; set; }
        public ICollection<Alert> Alerts { get; set; }
        public ICollection<StudentSchedule> StudentSchedules { get; set; }
        public ICollection<QuizResult> QuizResults { get; set; }
    }
}
