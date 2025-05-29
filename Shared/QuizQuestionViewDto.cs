using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizQuestionViewDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public int? CorrectAnswerIndex { get; set; } // Null for student view
        public int? SelectedAnswerIndex { get; set; } // For student submissions
    }
}
