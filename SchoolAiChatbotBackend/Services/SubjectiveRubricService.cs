using System.Text.Json;
using SchoolAiChatbotBackend.DTOs;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Repositories;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for managing subjective question marking rubrics.
    /// Handles serialization/deserialization of step rubrics and provides
    /// default/AI-generated rubric creation.
    /// </summary>
    public class SubjectiveRubricService : ISubjectiveRubricService
    {
        private readonly ISubjectiveRubricRepository _repository;
        private readonly ILogger<SubjectiveRubricService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public SubjectiveRubricService(
            ISubjectiveRubricRepository repository,
            ILogger<SubjectiveRubricService> logger)
        {
            _repository = repository;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task SaveRubricAsync(RubricCreateRequest request)
        {
            // Convert DTO steps to model steps
            var steps = request.Steps.Select(s => new StepRubricItem
            {
                StepNumber = s.StepNumber,
                Description = s.Description,
                Marks = s.Marks
            }).ToList();

            // Serialize steps to JSON
            var stepsJson = JsonSerializer.Serialize(steps, _jsonOptions);

            var entity = new SubjectiveRubric
            {
                ExamId = request.ExamId,
                QuestionId = request.QuestionId,
                TotalMarks = request.TotalMarks,
                StepsJson = stepsJson,
                QuestionText = request.QuestionText,
                ModelAnswer = request.ModelAnswer,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.SaveRubricAsync(entity);
            _logger.LogInformation("Saved rubric for Exam={ExamId}, Question={QuestionId} with {StepCount} steps",
                request.ExamId, request.QuestionId, steps.Count);
        }

        public async Task<List<StepRubricItem>?> GetRubricStepsAsync(string examId, string questionId)
        {
            var rubric = await _repository.GetRubricAsync(examId, questionId);
            if (rubric == null)
            {
                _logger.LogWarning("No rubric found for Exam={ExamId}, Question={QuestionId}", examId, questionId);
                return null;
            }

            return DeserializeSteps(rubric.StepsJson);
        }

        public async Task<RubricResponseDto?> GetRubricAsync(string examId, string questionId)
        {
            var rubric = await _repository.GetRubricAsync(examId, questionId);
            if (rubric == null)
                return null;

            return MapToDto(rubric);
        }

        public async Task<List<RubricResponseDto>> GetRubricsForExamAsync(string examId)
        {
            var rubrics = await _repository.GetRubricsForExamAsync(examId);
            return rubrics.Select(MapToDto).ToList();
        }

        public async Task SaveRubricsBatchAsync(string examId, List<QuestionRubricDto> rubrics)
        {
            var entities = rubrics.Select(r =>
            {
                var steps = r.Steps.Select(s => new StepRubricItem
                {
                    StepNumber = s.StepNumber,
                    Description = s.Description,
                    Marks = s.Marks
                }).ToList();

                return new SubjectiveRubric
                {
                    ExamId = examId,
                    QuestionId = r.QuestionId,
                    TotalMarks = r.TotalMarks,
                    StepsJson = JsonSerializer.Serialize(steps, _jsonOptions),
                    QuestionText = r.QuestionText,
                    ModelAnswer = r.ModelAnswer,
                    CreatedAt = DateTime.UtcNow
                };
            }).ToList();

            await _repository.SaveRubricsBatchAsync(entities);
            _logger.LogInformation("Batch saved {Count} rubrics for Exam={ExamId}", entities.Count, examId);
        }

        /// <summary>
        /// Generate a default rubric based on total marks.
        /// Simple approach: Number of steps = total marks, each step = 1 mark.
        /// Example: 2 marks question = 2 steps, each worth 1 mark.
        /// </summary>
        public Task<List<StepRubricItem>> GenerateDefaultRubricAsync(string questionText, string modelAnswer, int totalMarks)
        {
            var steps = new List<StepRubricItem>();

            // Step descriptions based on total marks
            var stepDescriptions = GetStepDescriptions(totalMarks);

            // Create one step per mark, each step worth 1 mark
            for (int i = 1; i <= totalMarks; i++)
            {
                steps.Add(new StepRubricItem
                {
                    StepNumber = i,
                    Description = stepDescriptions.Length >= i ? stepDescriptions[i - 1] : $"Step {i} - Correct working",
                    Marks = 1
                });
            }

            _logger.LogDebug("Generated default rubric with {StepCount} steps for {TotalMarks} marks (1 mark each)",
                steps.Count, totalMarks);

            return Task.FromResult(steps);
        }

        /// <summary>
        /// Get step descriptions based on total marks.
        /// </summary>
        private string[] GetStepDescriptions(int totalMarks)
        {
            return totalMarks switch
            {
                1 => new[] { "Correct answer with proper format" },
                2 => new[] { "Correct method/formula identification", "Correct final answer" },
                3 => new[] { "Identify correct formula/theorem", "Apply method correctly", "Correct final answer" },
                4 => new[] { "Identify concept/formula", "Initial setup with correct values", "Complete calculation", "Final answer with proper notation" },
                5 => new[] { "Identify concept/formula", "Initial setup", "Working step 1", "Working step 2", "Final answer" },
                _ => Enumerable.Range(1, totalMarks).Select(i => i switch
                {
                    1 => "Identify correct concept/formula/theorem",
                    _ when i == totalMarks => "Final answer with correct units/notation",
                    _ when i == totalMarks - 1 => "Complete calculation/derivation",
                    _ => $"Working step {i - 1} - Show intermediate calculation"
                }).ToArray()
            };
        }

        /// <summary>
        /// Generate rubric using AI (LLM).
        /// TODO: Implement AI-based rubric generation using Azure OpenAI.
        /// For now, falls back to default rubric generation.
        /// </summary>
        public async Task<List<StepRubricItem>> GenerateAIRubricAsync(string questionText, string modelAnswer, int totalMarks)
        {
            // TODO: Implement AI-based rubric generation
            // The AI prompt would be something like:
            // "You are a Karnataka State Board examiner. For the following question worth {totalMarks} marks,
            //  create a step-by-step marking rubric. Each step should have a description and marks.
            //  Question: {questionText}
            //  Model Answer: {modelAnswer}
            //  Return JSON: [{\"stepNumber\": 1, \"description\": \"...\", \"marks\": N}, ...]"

            _logger.LogInformation("AI rubric generation not yet implemented, using default rubric");

            // Fall back to default for now
            return await GenerateDefaultRubricAsync(questionText, modelAnswer, totalMarks);
        }

        public async Task DeleteRubricAsync(string examId, string questionId)
        {
            var rubric = await _repository.GetRubricAsync(examId, questionId);
            if (rubric != null)
            {
                await _repository.DeleteRubricAsync(rubric.Id);
                _logger.LogInformation("Deleted rubric for Exam={ExamId}, Question={QuestionId}", examId, questionId);
            }
        }

        #region Private Helper Methods

        private List<StepRubricItem> DeserializeSteps(string stepsJson)
        {
            try
            {
                return JsonSerializer.Deserialize<List<StepRubricItem>>(stepsJson, _jsonOptions)
                       ?? new List<StepRubricItem>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize rubric steps JSON: {Json}", stepsJson);
                return new List<StepRubricItem>();
            }
        }

        private RubricResponseDto MapToDto(SubjectiveRubric rubric)
        {
            var steps = DeserializeSteps(rubric.StepsJson);

            return new RubricResponseDto
            {
                Id = rubric.Id,
                ExamId = rubric.ExamId,
                QuestionId = rubric.QuestionId,
                TotalMarks = rubric.TotalMarks,
                Steps = steps.Select(s => new StepRubricItemDto
                {
                    StepNumber = s.StepNumber,
                    Description = s.Description,
                    Marks = s.Marks
                }).ToList(),
                QuestionText = rubric.QuestionText,
                ModelAnswer = rubric.ModelAnswer,
                CreatedAt = rubric.CreatedAt
            };
        }

        #endregion
    }
}
