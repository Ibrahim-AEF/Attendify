using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class QuizGenerationRequest
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string CourseCode { get; set; }
        [Required]
        public string Date { get; set; }
        [Required]
        public string Time { get; set; }
        [Required]
        public string Duration { get; set; }

        [FromForm(Name = "total_marks")]
        [Required]
        public string TotalMarks { get; set; }

        [Required]
        [Range(1, 50)]
        public int QuestionQuantity { get; set; }
        [Required]
        public QuestionType QuestionType { get; set; }
        [Required]
        public IFormFile LectureFile { get; set; }
    }
}
