using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class QuizResult
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        public DateTime CompletionTime { get; set; }
        public decimal Score { get; set; }
        public decimal TotalMarks { get; set; }
        public bool Passed { get; set; }
        public List<StudentAnswer> Answers { get; set; } = new();
    }
}
