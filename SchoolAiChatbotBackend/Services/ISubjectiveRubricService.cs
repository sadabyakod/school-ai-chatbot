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
        /// Get rubric steps for evaluation (from SQL - legacy)
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
        /// Generate a default rubric based on question characteristics.
        /// VALIDATION: Ensures Sum(step.Marks) == TotalMarks (throws if mismatch)
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

        /// <summary>
        /// Load frozen rubric from Azure Blob Storage.
        /// This is the ONLY source of truth for evaluation.
        /// Returns null if not found.
        /// </summary>
        Task<FrozenRubric?> GetFrozenRubricFromBlobAsync(string examId, string questionId);
    }

    /// <summary>
    /// Frozen rubric loaded from blob storage - the canonical source of truth for evaluation.
    /// </summary>
    public class FrozenRubric
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public int TotalMarks { get; set; }
        public List<FrozenRubricStep> Rubric { get; set; } = new();
        public string ModelAnswer { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// A single step in the frozen rubric
    /// </summary>
    public class FrozenRubricStep
    {
        public int StepNo { get; set; }
        public string Expected { get; set; } = string.Empty;
        public int Marks { get; set; }
    }
}
