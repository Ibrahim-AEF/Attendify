using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class NotificationDto
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string CourseCode { get; set; } // Optional
    }
}
