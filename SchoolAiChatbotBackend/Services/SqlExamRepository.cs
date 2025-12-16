using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// SQL Database-backed implementation of exam repository
    /// Uses WrittenSubmissions table shared with Azure Functions
    /// </summary>
    public class SqlExamRepository : IExamRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<SqlExamRepository> _logger;

        // Keep in-memory storage for MCQ data (not in SQL yet)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, McqSubmission> _mcqSubmissions = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, McqExtraction> _mcqExtractions = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, McqEvaluationFromSheet> _mcqEvaluationsFromSheets = new();

        public SqlExamRepository(AppDbContext dbContext, ILogger<SqlExamRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        #region MCQ Submissions (In-Memory - same as before)

        public Task<string> SaveMcqSubmissionAsync(McqSubmission submission)
        {
            var key = $"{submission.ExamId}_{submission.StudentId}";
            _mcqSubmissions[key] = submission;

            _logger.LogInformation(
                "Saved MCQ submission {SubmissionId} for exam {ExamId}, student {StudentId}",
                submission.McqSubmissionId,
                submission.ExamId,
                submission.StudentId);

            return Task.FromResult(submission.McqSubmissionId);
        }

        public Task<McqSubmission?> GetMcqSubmissionAsync(string examId, string studentId)
        {
            var key = $"{examId}_{studentId}";
            _mcqSubmissions.TryGetValue(key, out var submission);
            return Task.FromResult(submission);
        }

        public Task<List<McqSubmission>> GetAllMcqSubmissionsByExamAsync(string examId)
        {
            var submissions = _mcqSubmissions.Values
                .Where(s => s.ExamId == examId)
                .ToList();
            return Task.FromResult(submissions);
        }

        public Task<List<McqSubmission>> GetAllMcqSubmissionsByStudentAsync(string studentId)
        {
            var submissions = _mcqSubmissions.Values
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();
            return Task.FromResult(submissions);
        }

        #endregion

        #region Written Submissions (SQL Database)

        public async Task<string> SaveWrittenSubmissionAsync(WrittenSubmission submission)
        {
            try
            {
                // Ensure Id is set
                if (submission.Id == Guid.Empty)
                {
                    submission.Id = Guid.NewGuid();
                }

                _dbContext.WrittenSubmissions.Add(submission);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "[SQL] Saved written submission {SubmissionId} for exam {ExamId}, student {StudentId}",
                    submission.WrittenSubmissionId,
                    submission.ExamId,
                    submission.StudentId);

                return submission.WrittenSubmissionId;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true ||
                                                ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                // Handle duplicate - return existing
                var existing = await GetWrittenSubmissionByExamAndStudentAsync(submission.ExamId, submission.StudentId);
                if (existing != null)
                {
                    _logger.LogWarning(
                        "[SQL] Duplicate submission detected for exam {ExamId}, student {StudentId}. Returning existing {SubmissionId}",
                        submission.ExamId, submission.StudentId, existing.WrittenSubmissionId);
                    return existing.WrittenSubmissionId;
                }
                throw;
            }
        }

        public async Task<WrittenSubmission?> GetWrittenSubmissionAsync(string writtenSubmissionId)
        {
            if (!Guid.TryParse(writtenSubmissionId, out var guid))
            {
                _logger.LogWarning("[SQL] Invalid GUID format for submission ID: {SubmissionId}", writtenSubmissionId);
                return null;
            }

            return await _dbContext.WrittenSubmissions.FindAsync(guid);
        }

        public async Task<WrittenSubmission?> GetWrittenSubmissionByExamAndStudentAsync(string examId, string studentId)
        {
            return await _dbContext.WrittenSubmissions
                .FirstOrDefaultAsync(s => s.ExamId == examId && s.StudentId == studentId);
        }

        public async Task UpdateWrittenSubmissionStatusAsync(string writtenSubmissionId, SubmissionStatus status)
        {
            if (!Guid.TryParse(writtenSubmissionId, out var guid))
            {
                _logger.LogWarning("[SQL] Invalid GUID format for status update: {SubmissionId}", writtenSubmissionId);
                return;
            }

            var submission = await _dbContext.WrittenSubmissions.FindAsync(guid);
            if (submission != null)
            {
                var previousStatus = submission.Status;
                submission.Status = status;

                // Update relevant timestamps based on status (using numeric values to avoid duplicate case labels)
                switch ((int)status)
                {
                    case 0: // Uploaded/PendingEvaluation
                        break;
                    case 1: // OcrComplete/OcrProcessing/Evaluating
                        submission.OcrCompletedAt ??= DateTime.UtcNow;
                        submission.EvaluationStartedAt = DateTime.UtcNow;
                        break;
                    case 2: // EvaluationComplete/Completed
                        submission.EvaluatedAt = DateTime.UtcNow;
                        break;
                    case 3: // OcrFailed
                        submission.EvaluatedAt = DateTime.UtcNow;
                        break;
                    case 4: // EvaluationFailed/Failed
                        submission.EvaluatedAt = DateTime.UtcNow;
                        break;
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "[SQL] Updated written submission {SubmissionId} status: {PreviousStatus} → {NewStatus}",
                    writtenSubmissionId,
                    previousStatus,
                    status);
            }
            else
            {
                _logger.LogWarning("[SQL] Submission not found for status update: {SubmissionId}", writtenSubmissionId);
            }
        }

        public async Task UpdateWrittenSubmissionOcrTextAsync(string writtenSubmissionId, string ocrText)
        {
            if (!Guid.TryParse(writtenSubmissionId, out var guid))
            {
                _logger.LogWarning("[SQL] Invalid GUID format for OCR update: {SubmissionId}", writtenSubmissionId);
                return;
            }

            var submission = await _dbContext.WrittenSubmissions.FindAsync(guid);
            if (submission != null)
            {
                submission.ExtractedText = ocrText;
                submission.OcrCompletedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "[SQL] Updated written submission {SubmissionId} with OCR text ({Length} characters)",
                    writtenSubmissionId,
                    ocrText?.Length ?? 0);
            }
        }

        public async Task<List<WrittenSubmission>> GetAllWrittenSubmissionsByExamAsync(string examId)
        {
            return await _dbContext.WrittenSubmissions
                .Where(s => s.ExamId == examId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<WrittenSubmission>> GetAllWrittenSubmissionsByStudentAsync(string studentId)
        {
            return await _dbContext.WrittenSubmissions
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        #endregion

        #region Subjective Evaluations (SQL Database via WrittenQuestionEvaluations)

        public async Task SaveSubjectiveEvaluationsAsync(string writtenSubmissionId, List<SubjectiveEvaluationResult> evaluations)
        {
            if (!Guid.TryParse(writtenSubmissionId, out var submissionGuid))
            {
                _logger.LogWarning("[SQL] Invalid GUID format for saving evaluations: {SubmissionId}", writtenSubmissionId);
                return;
            }

            // Convert SubjectiveEvaluationResult to WrittenQuestionEvaluation entities
            foreach (var eval in evaluations)
            {
                var dbEval = new WrittenQuestionEvaluation
                {
                    Id = Guid.NewGuid(),
                    WrittenSubmissionId = submissionGuid,
                    QuestionId = eval.QuestionId,
                    QuestionNumber = eval.QuestionNumber,
                    ExtractedAnswer = eval.StudentAnswerEcho,
                    ModelAnswer = eval.ExpectedAnswer,
                    MaxScore = (decimal)eval.MaxMarks,
                    AwardedScore = (decimal)eval.EarnedMarks,
                    Feedback = eval.OverallFeedback,
                    RubricBreakdown = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        StepAnalysis = eval.StepAnalysis,
                        IsFullyCorrect = eval.IsFullyCorrect
                    }),
                    EvaluatedAt = eval.EvaluatedAt
                };

                _dbContext.WrittenQuestionEvaluations.Add(dbEval);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "[SQL] Saved {Count} subjective evaluations for written submission {SubmissionId}",
                evaluations.Count,
                writtenSubmissionId);
        }

        public async Task<List<SubjectiveEvaluationResult>> GetSubjectiveEvaluationsAsync(string writtenSubmissionId)
        {
            if (!Guid.TryParse(writtenSubmissionId, out var submissionGuid))
            {
                _logger.LogWarning("[SQL] Invalid GUID format for getting evaluations: {SubmissionId}", writtenSubmissionId);
                return new List<SubjectiveEvaluationResult>();
            }

            var dbEvaluations = await _dbContext.WrittenQuestionEvaluations
                .Where(e => e.WrittenSubmissionId == submissionGuid)
                .OrderBy(e => e.QuestionNumber)
                .ToListAsync();

            // Convert back to SubjectiveEvaluationResult
            return dbEvaluations.Select(e => new SubjectiveEvaluationResult
            {
                EvaluationId = e.Id.ToString(),
                WrittenSubmissionId = writtenSubmissionId,
                QuestionId = e.QuestionId,
                QuestionNumber = e.QuestionNumber,
                EarnedMarks = (double)e.AwardedScore,
                MaxMarks = (double)e.MaxScore,
                ExpectedAnswer = e.ModelAnswer,
                StudentAnswerEcho = e.ExtractedAnswer,
                OverallFeedback = e.Feedback,
                EvaluatedAt = e.EvaluatedAt,
                StepAnalysis = DeserializeStepAnalysis(e.RubricBreakdown)
            }).ToList();
        }

        private List<StepAnalysis> DeserializeStepAnalysis(string? rubricBreakdown)
        {
            if (string.IsNullOrEmpty(rubricBreakdown))
                return new List<StepAnalysis>();

            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(rubricBreakdown);
                if (doc.RootElement.TryGetProperty("StepAnalysis", out var stepElement))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<StepAnalysis>>(stepElement.GetRawText())
                        ?? new List<StepAnalysis>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize step analysis from rubric breakdown");
            }

            return new List<StepAnalysis>();
        }

        #endregion

        #region MCQ Extraction (In-Memory - same as before)

        public Task<string> SaveMcqExtractionAsync(McqExtraction extraction)
        {
            _mcqExtractions[extraction.WrittenSubmissionId] = extraction;

            _logger.LogInformation(
                "Saved MCQ extraction {ExtractionId} for written submission {SubmissionId} with {Count} answers",
                extraction.McqExtractionId,
                extraction.WrittenSubmissionId,
                extraction.ExtractedAnswers.Count);

            return Task.FromResult(extraction.McqExtractionId);
        }

        public Task<McqExtraction?> GetMcqExtractionAsync(string writtenSubmissionId)
        {
            _mcqExtractions.TryGetValue(writtenSubmissionId, out var extraction);
            return Task.FromResult(extraction);
        }

        public Task<string> SaveMcqEvaluationFromSheetAsync(McqEvaluationFromSheet evaluation)
        {
            var key = $"{evaluation.ExamId}_{evaluation.StudentId}";
            _mcqEvaluationsFromSheets[key] = evaluation;

            _logger.LogInformation(
                "Saved MCQ evaluation from sheet {EvaluationId} for exam {ExamId}, student {StudentId}: Score {Score}/{Total}",
                evaluation.McqEvaluationId,
                evaluation.ExamId,
                evaluation.StudentId,
                evaluation.TotalScore,
                evaluation.TotalMarks);

            return Task.FromResult(evaluation.McqEvaluationId);
        }

        public Task<McqEvaluationFromSheet?> GetMcqEvaluationFromSheetAsync(string examId, string studentId)
        {
            var key = $"{examId}_{studentId}";
            _mcqEvaluationsFromSheets.TryGetValue(key, out var evaluation);
            return Task.FromResult(evaluation);
        }

        #endregion

        #region Analytics & Reporting

        public async Task<List<(McqSubmission?, WrittenSubmission?)>> GetAllSubmissionsByExamAsync(string examId)
        {
            var mcqSubmissions = _mcqSubmissions.Values
                .Where(s => s.ExamId == examId)
                .ToList();

            var writtenSubmissions = await _dbContext.WrittenSubmissions
                .Where(s => s.ExamId == examId)
                .ToListAsync();

            // Get all unique student IDs
            var studentIds = mcqSubmissions.Select(s => s.StudentId)
                .Union(writtenSubmissions.Select(s => s.StudentId))
                .Distinct()
                .ToList();

            // Combine submissions for each student
            return studentIds.Select(studentId =>
            {
                var mcq = mcqSubmissions.FirstOrDefault(s => s.StudentId == studentId);
                var written = writtenSubmissions.FirstOrDefault(s => s.StudentId == studentId);
                return (mcq, written);
            }).ToList();
        }

        public async Task<List<string>> GetAllStudentIdsByExamAsync(string examId)
        {
            var mcqStudents = _mcqSubmissions.Values
                .Where(s => s.ExamId == examId)
                .Select(s => s.StudentId);

            var writtenStudents = await _dbContext.WrittenSubmissions
                .Where(s => s.ExamId == examId)
                .Select(s => s.StudentId)
                .ToListAsync();

            return mcqStudents.Union(writtenStudents).Distinct().ToList();
        }

        #endregion
    }
}
