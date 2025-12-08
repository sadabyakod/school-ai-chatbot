using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Repositories
{
    /// <summary>
    /// EF Core implementation of SubjectiveRubric repository
    /// </summary>
    public class SubjectiveRubricRepository : ISubjectiveRubricRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SubjectiveRubricRepository> _logger;
        private static bool _tableInitialized = false;
        private static readonly object _initLock = new object();

        public SubjectiveRubricRepository(AppDbContext context, ILogger<SubjectiveRubricRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ensures the SubjectiveRubrics table exists in the database.
        /// </summary>
        private async Task EnsureTableExistsAsync()
        {
            if (_tableInitialized) return;

            lock (_initLock)
            {
                if (_tableInitialized) return;
                _tableInitialized = true;
            }

            const string createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SubjectiveRubrics' AND xtype='U')
                BEGIN
                    CREATE TABLE [dbo].[SubjectiveRubrics] (
                        [Id] INT IDENTITY(1,1) NOT NULL,
                        [ExamId] NVARCHAR(100) NOT NULL,
                        [QuestionId] NVARCHAR(50) NOT NULL,
                        [TotalMarks] INT NOT NULL DEFAULT 0,
                        [StepsJson] NVARCHAR(MAX) NOT NULL,
                        [QuestionText] NVARCHAR(MAX) NULL,
                        [ModelAnswer] NVARCHAR(MAX) NULL,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_SubjectiveRubrics] PRIMARY KEY CLUSTERED ([Id] ASC)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX [IX_SubjectiveRubrics_ExamId_QuestionId] 
                    ON [dbo].[SubjectiveRubrics] ([ExamId], [QuestionId]);

                    CREATE NONCLUSTERED INDEX [IX_SubjectiveRubrics_ExamId] 
                    ON [dbo].[SubjectiveRubrics] ([ExamId]);
                END";

            try
            {
                await _context.Database.ExecuteSqlRawAsync(createTableSql);
                _logger.LogInformation("SubjectiveRubrics table verified/created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Note during SubjectiveRubrics table check (may already exist)");
            }
        }

        public async Task SaveRubricAsync(SubjectiveRubric rubric)
        {
            // Ensure table exists before any operation
            await EnsureTableExistsAsync();

            // Check if rubric already exists for this exam/question
            var existing = await _context.SubjectiveRubrics
                .FirstOrDefaultAsync(r => r.ExamId == rubric.ExamId && r.QuestionId == rubric.QuestionId);

            if (existing != null)
            {
                // Update existing rubric
                existing.TotalMarks = rubric.TotalMarks;
                existing.StepsJson = rubric.StepsJson;
                existing.ModelAnswer = rubric.ModelAnswer;
                existing.QuestionText = rubric.QuestionText;
                existing.CreatedAt = DateTime.UtcNow;
                
                _context.SubjectiveRubrics.Update(existing);
                _logger.LogInformation("Updated existing rubric for Exam={ExamId}, Question={QuestionId}", 
                    rubric.ExamId, rubric.QuestionId);
            }
            else
            {
                // Insert new rubric
                rubric.CreatedAt = DateTime.UtcNow;
                await _context.SubjectiveRubrics.AddAsync(rubric);
                _logger.LogInformation("Created new rubric for Exam={ExamId}, Question={QuestionId}", 
                    rubric.ExamId, rubric.QuestionId);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<SubjectiveRubric?> GetRubricAsync(string examId, string questionId)
        {
            await EnsureTableExistsAsync();
            return await _context.SubjectiveRubrics
                .FirstOrDefaultAsync(r => r.ExamId == examId && r.QuestionId == questionId);
        }

        public async Task<List<SubjectiveRubric>> GetRubricsForExamAsync(string examId)
        {
            await EnsureTableExistsAsync();
            return await _context.SubjectiveRubrics
                .Where(r => r.ExamId == examId)
                .OrderBy(r => r.QuestionId)
                .ToListAsync();
        }

        public async Task DeleteRubricAsync(int id)
        {
            await EnsureTableExistsAsync();
            var rubric = await _context.SubjectiveRubrics.FindAsync(id);
            if (rubric != null)
            {
                _context.SubjectiveRubrics.Remove(rubric);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted rubric Id={Id}", id);
            }
        }

        public async Task DeleteRubricsForExamAsync(string examId)
        {
            await EnsureTableExistsAsync();
            var rubrics = await _context.SubjectiveRubrics
                .Where(r => r.ExamId == examId)
                .ToListAsync();

            if (rubrics.Any())
            {
                _context.SubjectiveRubrics.RemoveRange(rubrics);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} rubrics for Exam={ExamId}", rubrics.Count, examId);
            }
        }

        public async Task<bool> RubricExistsAsync(string examId, string questionId)
        {
            await EnsureTableExistsAsync();
            return await _context.SubjectiveRubrics
                .AnyAsync(r => r.ExamId == examId && r.QuestionId == questionId);
        }

        public async Task SaveRubricsBatchAsync(List<SubjectiveRubric> rubrics)
        {
            if (rubrics == null || !rubrics.Any())
                return;

            // Ensure table exists before any operation
            await EnsureTableExistsAsync();

            var examId = rubrics.First().ExamId;
            
            // Delete existing rubrics for this exam first
            await DeleteRubricsForExamAsync(examId);

            // Insert all new rubrics
            foreach (var rubric in rubrics)
            {
                rubric.CreatedAt = DateTime.UtcNow;
            }

            await _context.SubjectiveRubrics.AddRangeAsync(rubrics);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Batch saved {Count} rubrics for Exam={ExamId}", rubrics.Count, examId);
        }
    }
}
