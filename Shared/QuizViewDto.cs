using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizViewDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalMarks { get; set; }
        public List<QuizQuestionViewDto> Questions { get; set; }
    }
}
