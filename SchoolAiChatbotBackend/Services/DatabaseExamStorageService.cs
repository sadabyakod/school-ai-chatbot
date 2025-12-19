using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Database-backed implementation of IExamStorageService using Azure SQL via EF Core.
    /// Exams are persisted to the database and survive server restarts.
    /// </summary>
    public class DatabaseExamStorageService : IExamStorageService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseExamStorageService> _logger;
        private static bool _tableInitialized = false;
        private static readonly object _initLock = new object();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public DatabaseExamStorageService(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseExamStorageService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Ensures the GeneratedExams table exists in the database.
        /// Called before any database operation.
        /// </summary>
        private async Task EnsureTableExistsAsync(AppDbContext context)
        {
            if (_tableInitialized) return;

            // Use lock for thread-safety but don't set flag until AFTER success
            bool shouldInitialize = false;
            lock (_initLock)
            {
                if (!_tableInitialized)
                {
                    shouldInitialize = true;
                }
            }
            
            if (!shouldInitialize) return;

            const string createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GeneratedExams' AND xtype='U')
                BEGIN
                    CREATE TABLE [dbo].[GeneratedExams] (
                        [Id] INT IDENTITY(1,1) NOT NULL,
                        [ExamId] NVARCHAR(200) NOT NULL,
                        [Subject] NVARCHAR(100) NOT NULL,
                        [Grade] NVARCHAR(50) NULL,
                        [Chapter] NVARCHAR(200) NULL,
                        [Difficulty] NVARCHAR(50) NULL,
                        [TotalMarks] INT NOT NULL DEFAULT 0,
                        [DurationMinutes] INT NOT NULL DEFAULT 0,
                        [ExamContentJson] NVARCHAR(MAX) NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [CreatedBy] NVARCHAR(100) NULL,
                        [IsActive] BIT NOT NULL DEFAULT 1,
                        CONSTRAINT [PK_GeneratedExams] PRIMARY KEY CLUSTERED ([Id] ASC)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX [IX_GeneratedExams_ExamId] 
                    ON [dbo].[GeneratedExams] ([ExamId]);

                    CREATE NONCLUSTERED INDEX [IX_GeneratedExams_Subject_Grade_Chapter] 
                    ON [dbo].[GeneratedExams] ([Subject], [Grade], [Chapter]);

                    CREATE NONCLUSTERED INDEX [IX_GeneratedExams_CreatedAt] 
                    ON [dbo].[GeneratedExams] ([CreatedAt] DESC);
                END";

            try
            {
                await context.Database.ExecuteSqlRawAsync(createTableSql);
                _logger.LogInformation("GeneratedExams table verified/created successfully");
                
                // Only set flag after successful initialization
                lock (_initLock)
                {
                    _tableInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Note during GeneratedExams table check (may already exist) - will retry on next request");
                // Don't set flag - will retry on next request
            }
        }

        /// <inheritdoc />
        public async Task StoreExamAsync(GeneratedExamResponse exam, string? createdBy = null)
        {
            if (exam == null || string.IsNullOrWhiteSpace(exam.ExamId))
            {
                _logger.LogWarning("Attempted to store null exam or exam with empty ID");
                Console.WriteLine("⚠️ StoreExamAsync: null exam or empty ID");
                return;
            }

            Console.WriteLine($"💾 StoreExamAsync: Starting storage for {exam.ExamId}");
            
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Console.WriteLine($"💾 StoreExamAsync: Got DbContext, ensuring table exists...");
                
                // Ensure table exists before any operation
                await EnsureTableExistsAsync(context);
                
                Console.WriteLine($"💾 StoreExamAsync: Table check complete, checking for existing exam...");

                // Check if exam already exists
                var existingExam = await context.GeneratedExams
                    .FirstOrDefaultAsync(e => e.ExamId == exam.ExamId);

                var examContentJson = JsonSerializer.Serialize(exam, _jsonOptions);
                Console.WriteLine($"💾 StoreExamAsync: JSON serialized, size={examContentJson.Length} chars");

                if (existingExam != null)
                {
                    // Update existing exam
                    existingExam.Subject = exam.Subject ?? string.Empty;
                    existingExam.Grade = exam.Grade;
                    existingExam.Chapter = exam.Chapter;
                    existingExam.Difficulty = exam.Difficulty;
                    existingExam.TotalMarks = exam.TotalMarks;
                    existingExam.DurationMinutes = exam.Duration;
                    existingExam.ExamContentJson = examContentJson;
                    existingExam.IsActive = true;

                    _logger.LogInformation(
                        "Updated existing exam {ExamId} in database",
                        exam.ExamId);
                }
                else
                {
                    // Create new exam
                    var generatedExam = new GeneratedExam
                    {
                        ExamId = exam.ExamId,
                        Subject = exam.Subject ?? string.Empty,
                        Grade = exam.Grade,
                        Chapter = exam.Chapter,
                        Difficulty = exam.Difficulty,
                        TotalMarks = exam.TotalMarks,
                        DurationMinutes = exam.Duration,
                        ExamContentJson = examContentJson,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = createdBy,
                        IsActive = true
                    };

                    context.GeneratedExams.Add(generatedExam);
                    Console.WriteLine($"💾 StoreExamAsync: Added to context, saving...");

                    _logger.LogInformation(
                        "Stored new exam {ExamId} - Subject: {Subject}, Chapter: {Chapter}, TotalMarks: {TotalMarks}",
                        exam.ExamId, exam.Subject, exam.Chapter, exam.TotalMarks);
                }

                await context.SaveChangesAsync();
                Console.WriteLine($"✅ StoreExamAsync: SaveChangesAsync completed successfully for {exam.ExamId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ StoreExamAsync FAILED for {exam.ExamId}: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}");
                _logger.LogError(ex, "Failed to store exam {ExamId} in database", exam.ExamId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<GeneratedExamResponse?> GetExamAsync(string examId)
        {
            if (string.IsNullOrWhiteSpace(examId))
            {
                return null;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Ensure table exists before any operation
                await EnsureTableExistsAsync(context);

                var exam = await context.GeneratedExams
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.ExamId == examId && e.IsActive);

                if (exam == null)
                {
                    _logger.LogDebug("Exam {ExamId} not found in database", examId);
                    return null;
                }

                var response = JsonSerializer.Deserialize<GeneratedExamResponse>(
                    exam.ExamContentJson, _jsonOptions);

                _logger.LogDebug("Retrieved exam {ExamId} from database", examId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve exam {ExamId} from database", examId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExamExistsAsync(string examId)
        {
            if (string.IsNullOrWhiteSpace(examId))
            {
                return false;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                return await context.GeneratedExams
                    .AnyAsync(e => e.ExamId == examId && e.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if exam {ExamId} exists", examId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RemoveExamAsync(string examId)
        {
            if (string.IsNullOrWhiteSpace(examId))
            {
                return false;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var exam = await context.GeneratedExams
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                if (exam == null)
                {
                    return false;
                }

                // Soft delete - just mark as inactive
                exam.IsActive = false;
                await context.SaveChangesAsync();

                _logger.LogInformation("Deactivated exam {ExamId} in database", examId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove exam {ExamId} from database", examId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllExamIdsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Ensure table exists before any operation
                await EnsureTableExistsAsync(context);

                return await context.GeneratedExams
                    .Where(e => e.IsActive)
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => e.ExamId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all exam IDs from database");
                return Enumerable.Empty<string>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GeneratedExamResponse>> GetAllExamsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Ensure table exists before any operation
                await EnsureTableExistsAsync(context);

                var exams = await context.GeneratedExams
                    .Where(e => e.IsActive)
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => e.ExamContentJson)
                    .ToListAsync();

                var results = new List<GeneratedExamResponse>();
                foreach (var json in exams)
                {
                    try
                    {
                        var exam = JsonSerializer.Deserialize<GeneratedExamResponse>(json, _jsonOptions);
                        if (exam != null)
                        {
                            results.Add(exam);
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip invalid JSON entries
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all exams from database");
                return Enumerable.Empty<GeneratedExamResponse>();
            }
        }

        // Synchronous methods for backward compatibility
        // These call the async methods and block (not ideal, but maintains compatibility)

        /// <inheritdoc />
        public void StoreExam(GeneratedExamResponse exam)
        {
            StoreExamAsync(exam).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public GeneratedExamResponse? GetExam(string examId)
        {
            return GetExamAsync(examId).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public bool ExamExists(string examId)
        {
            return ExamExistsAsync(examId).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public bool RemoveExam(string examId)
        {
            return RemoveExamAsync(examId).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllExamIds()
        {
            return GetAllExamIdsAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public IEnumerable<GeneratedExamResponse> GetAllExams()
        {
            return GetAllExamsAsync().GetAwaiter().GetResult();
        }
    }
}
