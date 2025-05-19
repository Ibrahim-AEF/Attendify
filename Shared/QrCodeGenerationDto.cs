using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QrCodeGenerationDto
    {
        public string CourseCode { get; set; }
        //public string LectureName { get; set; }
        //public DateTime LectureDate { get; set; }
        public int ScheduleId { get; set; } // ID of the schedule (lecture) to generate QR for
    }

}
