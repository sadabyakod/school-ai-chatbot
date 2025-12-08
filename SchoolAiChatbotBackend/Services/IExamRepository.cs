using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Repository for managing exam submissions and evaluations
    /// </summary>
    public interface IExamRepository
    {
        // MCQ Submissions
        Task<string> SaveMcqSubmissionAsync(McqSubmission submission);
        Task<McqSubmission?> GetMcqSubmissionAsync(string examId, string studentId);

        // Written Submissions
        Task<string> SaveWrittenSubmissionAsync(WrittenSubmission submission);
        Task<WrittenSubmission?> GetWrittenSubmissionAsync(string writtenSubmissionId);
        Task<WrittenSubmission?> GetWrittenSubmissionByExamAndStudentAsync(string examId, string studentId);
        Task UpdateWrittenSubmissionStatusAsync(string writtenSubmissionId, SubmissionStatus status);
        Task UpdateWrittenSubmissionOcrTextAsync(string writtenSubmissionId, string ocrText);

        // Subjective Evaluations
        Task SaveSubjectiveEvaluationsAsync(string writtenSubmissionId, List<SubjectiveEvaluationResult> evaluations);
        Task<List<SubjectiveEvaluationResult>> GetSubjectiveEvaluationsAsync(string writtenSubmissionId);

        // Analytics & Reporting
        Task<List<McqSubmission>> GetAllMcqSubmissionsByExamAsync(string examId);
        Task<List<WrittenSubmission>> GetAllWrittenSubmissionsByExamAsync(string examId);
        Task<List<(McqSubmission?, WrittenSubmission?)>> GetAllSubmissionsByExamAsync(string examId);
        Task<List<string>> GetAllStudentIdsByExamAsync(string examId);
        Task<List<McqSubmission>> GetAllMcqSubmissionsByStudentAsync(string studentId);
        Task<List<WrittenSubmission>> GetAllWrittenSubmissionsByStudentAsync(string studentId);
    }
}
