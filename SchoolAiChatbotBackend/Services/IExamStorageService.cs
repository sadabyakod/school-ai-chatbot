using SchoolAiChatbotBackend.Controllers;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for storing and retrieving generated exams.
    /// This provides a centralized storage mechanism that can be used
    /// by both ExamGeneratorController (to store exams) and 
    /// ExamSubmissionController (to retrieve exams for evaluation).
    /// </summary>
    public interface IExamStorageService
    {
        /// <summary>
        /// Store a generated exam for later retrieval.
        /// </summary>
        /// <param name="exam">The generated exam to store</param>
        /// <param name="createdBy">Optional: who created the exam</param>
        Task StoreExamAsync(GeneratedExamResponse exam, string? createdBy = null);

        /// <summary>
        /// Retrieve a stored exam by its ID.
        /// </summary>
        /// <param name="examId">The exam ID</param>
        /// <returns>The exam if found, null otherwise</returns>
        Task<GeneratedExamResponse?> GetExamAsync(string examId);

        /// <summary>
        /// Check if an exam exists in storage.
        /// </summary>
        /// <param name="examId">The exam ID</param>
        /// <returns>True if the exam exists</returns>
        Task<bool> ExamExistsAsync(string examId);

        /// <summary>
        /// Remove an exam from storage.
        /// </summary>
        /// <param name="examId">The exam ID to remove</param>
        /// <returns>True if the exam was removed</returns>
        Task<bool> RemoveExamAsync(string examId);

        /// <summary>
        /// Get all stored exam IDs.
        /// </summary>
        /// <returns>Collection of exam IDs</returns>
        Task<IEnumerable<string>> GetAllExamIdsAsync();

        /// <summary>
        /// Get all stored exams.
        /// </summary>
        /// <returns>Collection of stored exams</returns>
        Task<IEnumerable<GeneratedExamResponse>> GetAllExamsAsync();

        // Synchronous methods for backward compatibility (call async internally)
        void StoreExam(GeneratedExamResponse exam);
        GeneratedExamResponse? GetExam(string examId);
        bool ExamExists(string examId);
        bool RemoveExam(string examId);
        IEnumerable<string> GetAllExamIds();
        IEnumerable<GeneratedExamResponse> GetAllExams();
    }
}
