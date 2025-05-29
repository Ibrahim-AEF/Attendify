using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service.Abstraction;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class AiQuizService : IAiQuizService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiQuizService> _logger;

        public AiQuizService(HttpClient httpClient, IConfiguration configuration , ILogger<AiQuizService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Configure base address from appsettings.json
            _httpClient.BaseAddress = new Uri(_configuration["AiQuizService:BaseUrl"]);
        }

        public async Task<GeneratedQuizResponse> GenerateQuizAsync(QuizGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Starting quiz generation for {Title}", request.Title);

                using var content = new MultipartFormDataContent();

                // Add file content
                var fileContent = new StreamContent(request.LectureFile.OpenReadStream());
                content.Add(fileContent, "file", request.LectureFile.FileName);

                // Add other parameters
                content.Add(new StringContent(request.Title), "title");
                content.Add(new StringContent(request.Date), "date");
                content.Add(new StringContent(request.Time), "time");
                content.Add(new StringContent(request.Duration), "duration");
                content.Add(new StringContent(request.TotalMarks), "total_marks");
                content.Add(new StringContent(request.QuestionQuantity.ToString()), "num_questions");
                content.Add(new StringContent(request.QuestionType.ToString()), "question_type");

                _logger.LogDebug("Sending request to AI service");
                var response = await _httpClient.PostAsync("/generate-quiz", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI service returned {StatusCode}: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"AI service error: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<GeneratedQuizResponse>();
                _logger.LogInformation("Successfully generated quiz with {Count} questions",
                    result?.Quiz?.Count(q => q == '\n') / 5); // Rough estimate

                return result ?? throw new InvalidOperationException("Null response from AI service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate quiz");
                throw;
            }
        }
    }
}
