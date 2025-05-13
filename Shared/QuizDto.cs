using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizDto
    {
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public int DurationMinutes { get; set; }
        public int PassMark { get; set; }
        public List<QuizQuestionDto> Questions { get; set; }
    }
}
