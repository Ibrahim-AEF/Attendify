using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Lecture
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CourseCode { get; set; }

        public int ScheduleId { get; set; } // Add this

        [Required]
        public DateTime Date { get; set; }
        public string LectureName { get; set; }

        public DateTime QRCodeExpiry { get; set; }

        public string QRCode { get; set; } 

        // Navigation properties
        public Course Course { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
        public Schedule Schedule { get; set; } // Add this navigation property
    }
}
