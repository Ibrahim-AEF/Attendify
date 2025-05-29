using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizQuestionResultDto
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public int SelectedAnswerIndex { get; set; }
        public bool IsCorrect { get; set; }
        public decimal Marks { get; set; }
        public decimal ObtainedMarks { get; set; }
    }
}
