using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class StudentSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        // Navigation properties
        public Student Student { get; set; }
        public Schedule Schedule { get; set; }
    }

}
