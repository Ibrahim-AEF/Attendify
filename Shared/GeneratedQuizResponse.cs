using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Shared
{
    public class GeneratedQuizResponse
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Duration { get; set; }

        [JsonPropertyName("total_marks")]
        public string TotalMarks { get; set; }
        public string QuestionType { get; set; }
        public string Quiz { get; set; }
    }
}
