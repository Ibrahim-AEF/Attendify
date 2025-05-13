using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Alert
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; } // Foreign key to Student

        [Required]
        public string Message { get; set; }

        public string CourseCode { get; set; } // Optional, for course-specific alerts

        [Required]
        public DateTime SentDate { get; set; }

        [Required]
        public bool IsRead { get; set; }

        // Navigation property
        public Student Student { get; set; }
    }
}
