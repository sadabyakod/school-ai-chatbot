using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Controllers;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// In-memory implementation of IExamStorageService using ConcurrentDictionary.
    /// This implementation is thread-safe and suitable for single-server deployments.
    /// 
    /// NOTE: For production, use DatabaseExamStorageService instead which persists to Azure SQL.
    /// This in-memory implementation loses all data on server restart.
    /// </summary>
    public class ExamStorageService : IExamStorageService
    {
        private readonly ConcurrentDictionary<string, GeneratedExamResponse> _exams = new();
        private readonly ILogger<ExamStorageService> _logger;

        public ExamStorageService(ILogger<ExamStorageService> logger)
        {
            _logger = logger;
        }

        // Async methods (return Task-wrapped sync results for in-memory)

        /// <inheritdoc />
        public Task StoreExamAsync(GeneratedExamResponse exam, string? createdBy = null)
        {
            StoreExam(exam);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<GeneratedExamResponse?> GetExamAsync(string examId)
        {
            return Task.FromResult(GetExam(examId));
        }

        /// <inheritdoc />
        public Task<bool> ExamExistsAsync(string examId)
        {
            return Task.FromResult(ExamExists(examId));
        }

        /// <inheritdoc />
        public Task<bool> RemoveExamAsync(string examId)
        {
            return Task.FromResult(RemoveExam(examId));
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetAllExamIdsAsync()
        {
            return Task.FromResult(GetAllExamIds());
        }

        /// <inheritdoc />
        public Task<IEnumerable<GeneratedExamResponse>> GetAllExamsAsync()
        {
            return Task.FromResult(GetAllExams());
        }

        // Sync methods

        /// <inheritdoc />
        public void StoreExam(GeneratedExamResponse exam)
        {
            if (exam == null)
            {
                _logger.LogWarning("Attempted to store null exam");
                return;
            }

            if (string.IsNullOrWhiteSpace(exam.ExamId))
            {
                _logger.LogWarning("Attempted to store exam with null/empty ExamId");
                return;
            }

            _exams[exam.ExamId] = exam;
            _logger.LogInformation(
                "Stored exam {ExamId} - Subject: {Subject}, Chapter: {Chapter}, Questions: {QuestionCount}",
                exam.ExamId,
                exam.Subject,
                exam.Chapter,
                exam.Questions?.Count ?? exam.Parts?.Sum(p => p.Questions?.Count ?? 0) ?? 0);
        }

        /// <inheritdoc />
        public GeneratedExamResponse? GetExam(string examId)
        {
            if (string.IsNullOrWhiteSpace(examId))
            {
                _logger.LogWarning("GetExam called with null/empty examId");
                return null;
            }

            if (_exams.TryGetValue(examId, out var exam))
            {
                _logger.LogDebug("Retrieved exam {ExamId}", examId);
                return exam;
            }

            _logger.LogWarning("Exam {ExamId} not found in storage", examId);
            return null;
        }

        /// <inheritdoc />
        public bool ExamExists(string examId)
        {
            if (string.IsNullOrWhiteSpace(examId))
            {
                return false;
            }

            return _exams.ContainsKey(examId);
        }

        /// <inheritdoc />
        public bool RemoveExam(string examId)
        {
            if (string.IsNullOrWhiteSpace(examId))
            {
                return false;
            }

            var removed = _exams.TryRemove(examId, out _);
            if (removed)
            {
                _logger.LogInformation("Removed exam {ExamId} from storage", examId);
            }
            return removed;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllExamIds()
        {
            return _exams.Keys.ToList();
        }

        /// <inheritdoc />
        public IEnumerable<GeneratedExamResponse> GetAllExams()
        {
            return _exams.Values.ToList();
        }
    }
}
