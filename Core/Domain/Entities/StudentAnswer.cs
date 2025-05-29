using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class StudentAnswer
    {
        public int Id { get; set; }
        public int QuizResultId { get; set; }
        public QuizResult QuizResult { get; set; }
        public int QuestionId { get; set; }
        public QuizQuestion Question { get; set; }
        public int SelectedAnswerIndex { get; set; }
    }
}
