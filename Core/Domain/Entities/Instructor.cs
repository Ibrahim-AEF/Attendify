using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Instructor
    {
        [Key]
        public string Id { get; set; } // Will be same as Identity UserId

        [Required]
        public string InstructorId { get; set; } // University ID

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Department { get; set; }

        public string OfficeAddress { get; set; }

        // Navigation properties
        public ICollection<Course> Courses { get; set; }
        public ICollection<Student> Students { get; set; }
    }
}
