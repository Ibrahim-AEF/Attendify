using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizDto
    {
        //public string Title { get; set; }
        //public string CourseCode { get; set; }
        //public int DurationMinutes { get; set; }
        //public int PassMark { get; set; }
        //public List<QuizQuestionDto> Questions { get; set; }
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Date { get; set; } // Format: "yyyy-MM-dd" or "dd/MM/yyyy"
        public string Time { get; set; } // Format: "HH:mm"
        public int DurationMinutes { get; set; }
        public int PassMark { get; set; } // Percentage (e.g., 50 for 50%)
        public List<QuizQuestionDto> Questions { get; set; } = new();
        public string RawQuiz { get; set; } // Original AI-generated content
    }
}
