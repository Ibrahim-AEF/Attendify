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

        [Required]
        public DateTime Date { get; set; }

        public string QRCode { get; set; } 

        // Navigation properties
        public Course Course { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
    }
}
