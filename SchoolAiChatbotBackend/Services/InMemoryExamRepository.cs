using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// In-memory implementation of exam repository (replace with database in production)
    /// </summary>
    public class InMemoryExamRepository : IExamRepository
    {
        // Static dictionaries to persist data across requests (in-memory only)
        private static readonly ConcurrentDictionary<string, McqSubmission> _mcqSubmissions = new();
        private static readonly ConcurrentDictionary<string, WrittenSubmission> _writtenSubmissions = new();
        private static readonly ConcurrentDictionary<string, List<SubjectiveEvaluationResult>> _subjectiveEvaluations = new();
        private static readonly ConcurrentDictionary<string, McqExtraction> _mcqExtractions = new();
        private static readonly ConcurrentDictionary<string, McqEvaluationFromSheet> _mcqEvaluationsFromSheets = new();
        private readonly ILogger<InMemoryExamRepository> _logger;

        public InMemoryExamRepository(ILogger<InMemoryExamRepository> logger)
        {
            _logger = logger;
        }

        // MCQ Submissions
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

        // Written Submissions
        public Task<string> SaveWrittenSubmissionAsync(WrittenSubmission submission)
        {
            _writtenSubmissions[submission.WrittenSubmissionId] = submission;

            _logger.LogInformation(
                "Saved written submission {SubmissionId} for exam {ExamId}, student {StudentId}",
                submission.WrittenSubmissionId,
                submission.ExamId,
                submission.StudentId);

            return Task.FromResult(submission.WrittenSubmissionId);
        }

        public Task<WrittenSubmission?> GetWrittenSubmissionAsync(string writtenSubmissionId)
        {
            _writtenSubmissions.TryGetValue(writtenSubmissionId, out var submission);
            return Task.FromResult(submission);
        }

        public Task<WrittenSubmission?> GetWrittenSubmissionByExamAndStudentAsync(string examId, string studentId)
        {
            var submission = _writtenSubmissions.Values
                .FirstOrDefault(s => s.ExamId == examId && s.StudentId == studentId);
            return Task.FromResult(submission);
        }

        public Task UpdateWrittenSubmissionStatusAsync(string writtenSubmissionId, SubmissionStatus status)
        {
            if (_writtenSubmissions.TryGetValue(writtenSubmissionId, out var submission))
            {
                submission.Status = status;
                if (status == SubmissionStatus.Completed)
                {
                    submission.EvaluatedAt = DateTime.UtcNow;
                }

                _logger.LogInformation(
                    "Updated written submission {SubmissionId} status to {Status}",
                    writtenSubmissionId,
                    status);
            }

            return Task.CompletedTask;
        }

        public Task UpdateWrittenSubmissionOcrTextAsync(string writtenSubmissionId, string ocrText)
        {
            if (_writtenSubmissions.TryGetValue(writtenSubmissionId, out var submission))
            {
                submission.OcrText = ocrText;

                _logger.LogInformation(
                    "Updated written submission {SubmissionId} with OCR text ({Length} characters)",
                    writtenSubmissionId,
                    ocrText?.Length ?? 0);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Complete evaluation with final results - updates TotalScore, MaxPossibleScore, Percentage, Grade,
        /// EvaluationResultBlobPath and sets Status to EvaluationComplete (2)
        /// </summary>
        public Task CompleteEvaluationWithResultsAsync(
            string writtenSubmissionId,
            decimal totalScore,
            decimal maxPossibleScore,
            string evaluationResultBlobPath,
            long? evaluationTimeMs = null)
        {
            if (_writtenSubmissions.TryGetValue(writtenSubmissionId, out var submission))
            {
                var percentage = maxPossibleScore > 0
                    ? Math.Round((totalScore / maxPossibleScore) * 100, 2)
                    : 0;
                var grade = percentage switch
                {
                    >= 90 => "A+",
                    >= 80 => "A",
                    >= 70 => "B+",
                    >= 60 => "B",
                    >= 50 => "C",
                    >= 40 => "D",
                    >= 35 => "E",
                    _ => "F"
                };

                submission.TotalScore = totalScore;
                submission.MaxPossibleScore = maxPossibleScore;
                submission.Percentage = percentage;
                submission.Grade = grade;
                submission.EvaluationResultBlobPath = evaluationResultBlobPath;
                submission.Status = SubmissionStatus.EvaluationComplete;
                submission.EvaluatedAt = DateTime.UtcNow;
                submission.EvaluationProcessingTimeMs = evaluationTimeMs;

                _logger.LogInformation(
                    "Completed evaluation for submission {SubmissionId}: Score={Score}/{Max} ({Percentage}%) Grade={Grade}",
                    writtenSubmissionId,
                    totalScore,
                    maxPossibleScore,
                    percentage,
                    grade);
            }

            return Task.CompletedTask;
        }

        // Subjective Evaluations
        public Task SaveSubjectiveEvaluationsAsync(string writtenSubmissionId, List<SubjectiveEvaluationResult> evaluations)
        {
            foreach (var evaluation in evaluations)
            {
                evaluation.WrittenSubmissionId = writtenSubmissionId;
            }

            _subjectiveEvaluations[writtenSubmissionId] = evaluations;

            _logger.LogInformation(
                "Saved {Count} subjective evaluations for written submission {SubmissionId}",
                evaluations.Count,
                writtenSubmissionId);

            return Task.CompletedTask;
        }

        public Task<List<SubjectiveEvaluationResult>> GetSubjectiveEvaluationsAsync(string writtenSubmissionId)
        {
            _subjectiveEvaluations.TryGetValue(writtenSubmissionId, out var evaluations);
            return Task.FromResult(evaluations ?? new List<SubjectiveEvaluationResult>());
        }

        // MCQ Extraction from Answer Sheets
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

        // Analytics & Reporting Methods
        public Task<List<McqSubmission>> GetAllMcqSubmissionsByExamAsync(string examId)
        {
            var submissions = _mcqSubmissions.Values
                .Where(s => s.ExamId == examId)
                .ToList();

            _logger.LogInformation("Retrieved {Count} MCQ submissions for exam {ExamId}", submissions.Count, examId);
            return Task.FromResult(submissions);
        }

        public Task<List<WrittenSubmission>> GetAllWrittenSubmissionsByExamAsync(string examId)
        {
            var submissions = _writtenSubmissions.Values
                .Where(s => s.ExamId == examId)
                .ToList();

            _logger.LogInformation("Retrieved {Count} written submissions for exam {ExamId}", submissions.Count, examId);
            return Task.FromResult(submissions);
        }

        public Task<List<(McqSubmission?, WrittenSubmission?)>> GetAllSubmissionsByExamAsync(string examId)
        {
            var mcqSubmissions = _mcqSubmissions.Values
                .Where(s => s.ExamId == examId)
                .ToList();

            var writtenSubmissions = _writtenSubmissions.Values
                .Where(s => s.ExamId == examId)
                .ToList();

            // Get all unique student IDs
            var studentIds = mcqSubmissions.Select(s => s.StudentId)
                .Union(writtenSubmissions.Select(s => s.StudentId))
                .Distinct()
                .ToList();

            // Combine submissions for each student
            var combinedSubmissions = studentIds.Select(studentId =>
            {
                var mcq = mcqSubmissions.FirstOrDefault(s => s.StudentId == studentId);
                var written = writtenSubmissions.FirstOrDefault(s => s.StudentId == studentId);
                return (mcq, written);
            }).ToList();

            _logger.LogInformation("Retrieved {Count} combined submissions for exam {ExamId}", combinedSubmissions.Count, examId);
            return Task.FromResult(combinedSubmissions);
        }

        public Task<List<string>> GetAllStudentIdsByExamAsync(string examId)
        {
            var mcqStudents = _mcqSubmissions.Values
                .Where(s => s.ExamId == examId)
                .Select(s => s.StudentId);

            var writtenStudents = _writtenSubmissions.Values
                .Where(s => s.ExamId == examId)
                .Select(s => s.StudentId);

            var allStudents = mcqStudents.Union(writtenStudents).Distinct().ToList();

            _logger.LogInformation("Retrieved {Count} unique students for exam {ExamId}", allStudents.Count, examId);
            return Task.FromResult(allStudents);
        }

        public Task<List<McqSubmission>> GetAllMcqSubmissionsByStudentAsync(string studentId)
        {
            var submissions = _mcqSubmissions.Values
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} MCQ submissions for student {StudentId}", submissions.Count, studentId);
            return Task.FromResult(submissions);
        }

        public Task<List<WrittenSubmission>> GetAllWrittenSubmissionsByStudentAsync(string studentId)
        {
            var submissions = _writtenSubmissions.Values
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} written submissions for student {StudentId}", submissions.Count, studentId);
            return Task.FromResult(submissions);
        }
    }
}
