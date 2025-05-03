using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AbsenceExcusal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; } // Foreign key to Student

        [Required]
        public int LectureId { get; set; } // Foreign key to Lecture

        [Required]
        public string Reason { get; set; }

        public bool? IsApproved { get; set; } // Null = pending, true = approved, false = rejected

        public DateTime RequestDate { get; set; }
        public DateTime? ReviewDate { get; set; }

        // Navigation properties
        public Student Student { get; set; }
        public Lecture Lecture { get; set; }
    }
}
