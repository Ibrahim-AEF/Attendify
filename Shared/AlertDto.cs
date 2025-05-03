using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class AlertDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string CourseCode { get; set; }
        public string SentDate { get; set; }
        public bool IsRead { get; set; }
        public string StudentName { get; set; }
        public string StudentId { get; set; }
    }
}
