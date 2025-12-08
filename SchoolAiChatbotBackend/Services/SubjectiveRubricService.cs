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
        /// Uses a simple heuristic to split marks across 3 steps:
        /// - Understanding/Setup (20% of marks)
        /// - Working/Calculation (50% of marks)
        /// - Final Answer/Conclusion (30% of marks)
        /// </summary>
        public Task<List<StepRubricItem>> GenerateDefaultRubricAsync(string questionText, string modelAnswer, int totalMarks)
        {
            var steps = new List<StepRubricItem>();

            if (totalMarks <= 2)
            {
                // Simple 2-step rubric for small mark questions
                steps.Add(new StepRubricItem
                {
                    StepNumber = 1,
                    Description = "Correct method/formula identification",
                    Marks = (int)Math.Ceiling(totalMarks / 2.0)
                });
                steps.Add(new StepRubricItem
                {
                    StepNumber = 2,
                    Description = "Correct final answer with proper format",
                    Marks = totalMarks - steps[0].Marks
                });
            }
            else if (totalMarks <= 5)
            {
                // 3-step rubric for medium mark questions
                var step1Marks = Math.Max(1, (int)Math.Round(totalMarks * 0.2));
                var step3Marks = Math.Max(1, (int)Math.Round(totalMarks * 0.3));
                var step2Marks = totalMarks - step1Marks - step3Marks;

                steps.Add(new StepRubricItem
                {
                    StepNumber = 1,
                    Description = "Identify correct formula/theorem/method",
                    Marks = step1Marks
                });
                steps.Add(new StepRubricItem
                {
                    StepNumber = 2,
                    Description = "Apply method correctly with proper working shown",
                    Marks = step2Marks
                });
                steps.Add(new StepRubricItem
                {
                    StepNumber = 3,
                    Description = "Arrive at correct final answer with proper notation",
                    Marks = step3Marks
                });
            }
            else
            {
                // 4-step rubric for higher mark questions (5+ marks)
                var step1Marks = Math.Max(1, (int)Math.Round(totalMarks * 0.15)); // Understanding
                var step2Marks = Math.Max(1, (int)Math.Round(totalMarks * 0.35)); // Working Part 1
                var step3Marks = Math.Max(1, (int)Math.Round(totalMarks * 0.30)); // Working Part 2
                var step4Marks = totalMarks - step1Marks - step2Marks - step3Marks; // Conclusion

                steps.Add(new StepRubricItem
                {
                    StepNumber = 1,
                    Description = "Identify correct concept/formula/theorem with proper statement",
                    Marks = step1Marks
                });
                steps.Add(new StepRubricItem
                {
                    StepNumber = 2,
                    Description = "Initial setup and substitution with correct values",
                    Marks = step2Marks
                });
                steps.Add(new StepRubricItem
                {
                    StepNumber = 3,
                    Description = "Complete calculation/derivation with all intermediate steps",
                    Marks = step3Marks
                });
                steps.Add(new StepRubricItem
                {
                    StepNumber = 4,
                    Description = "Final answer with correct units/notation and conclusion",
                    Marks = step4Marks
                });
            }

            _logger.LogDebug("Generated default rubric with {StepCount} steps for {TotalMarks} marks", 
                steps.Count, totalMarks);

            return Task.FromResult(steps);
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
