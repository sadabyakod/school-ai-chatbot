using SchoolAiChatbotBackend.DTOs;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service interface for managing subjective question rubrics
    /// </summary>
    public interface ISubjectiveRubricService
    {
        /// <summary>
        /// Save a rubric for a subjective question
        /// </summary>
        Task SaveRubricAsync(RubricCreateRequest request);

        /// <summary>
        /// Get rubric steps for evaluation
        /// </summary>
        Task<List<StepRubricItem>?> GetRubricStepsAsync(string examId, string questionId);

        /// <summary>
        /// Get full rubric details
        /// </summary>
        Task<RubricResponseDto?> GetRubricAsync(string examId, string questionId);

        /// <summary>
        /// Get all rubrics for an exam
        /// </summary>
        Task<List<RubricResponseDto>> GetRubricsForExamAsync(string examId);

        /// <summary>
        /// Save multiple rubrics at once (for exam generation)
        /// </summary>
        Task SaveRubricsBatchAsync(string examId, List<QuestionRubricDto> rubrics);

        /// <summary>
        /// Generate a default rubric based on question characteristics
        /// </summary>
        Task<List<StepRubricItem>> GenerateDefaultRubricAsync(string questionText, string modelAnswer, int totalMarks);

        /// <summary>
        /// Generate rubric using AI (for future enhancement)
        /// </summary>
        Task<List<StepRubricItem>> GenerateAIRubricAsync(string questionText, string modelAnswer, int totalMarks);

        /// <summary>
        /// Delete rubric
        /// </summary>
        Task DeleteRubricAsync(string examId, string questionId);
    }
}
