using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizResultDto
    {
        public decimal Score { get; set; }
        public decimal TotalMarks { get; set; }
        public bool Passed { get; set; }
        public decimal Percentage { get; set; }
        public List<QuizQuestionResultDto> Questions { get; set; }
    }
}
