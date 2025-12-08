using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Repositories
{
    /// <summary>
    /// Repository interface for SubjectiveRubric persistence
    /// </summary>
    public interface ISubjectiveRubricRepository
    {
        /// <summary>
        /// Save a new rubric or update existing one
        /// </summary>
        Task SaveRubricAsync(SubjectiveRubric rubric);

        /// <summary>
        /// Get rubric for a specific exam and question
        /// </summary>
        Task<SubjectiveRubric?> GetRubricAsync(string examId, string questionId);

        /// <summary>
        /// Get all rubrics for an exam
        /// </summary>
        Task<List<SubjectiveRubric>> GetRubricsForExamAsync(string examId);

        /// <summary>
        /// Delete rubric by ID
        /// </summary>
        Task DeleteRubricAsync(int id);

        /// <summary>
        /// Delete all rubrics for an exam
        /// </summary>
        Task DeleteRubricsForExamAsync(string examId);

        /// <summary>
        /// Check if a rubric exists
        /// </summary>
        Task<bool> RubricExistsAsync(string examId, string questionId);

        /// <summary>
        /// Batch save multiple rubrics
        /// </summary>
        Task SaveRubricsBatchAsync(List<SubjectiveRubric> rubrics);
    }
}
