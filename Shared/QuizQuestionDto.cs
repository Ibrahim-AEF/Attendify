using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizQuestionDto
    {
        //public string QuestionText { get; set; }
        //public List<string> Options { get; set; }
        //public int CorrectAnswerIndex { get; set; }

        public string QuestionText { get; set; }
        public List<string> Options { get; set; } = new();
        public int CorrectAnswerIndex { get; set; } // 0-based index
    }
}
