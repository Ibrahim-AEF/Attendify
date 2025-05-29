using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizSubmissionDto
    {
        public int QuizId { get; set; }
        public List<QuestionAnswerDto> Answers { get; set; }
    }
}
