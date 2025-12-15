using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Features.Exams;

namespace SchoolAiChatbotBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<School> Schools { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Faq> Faqs { get; set; }
        public DbSet<SyllabusChunk> SyllabusChunks { get; set; }
        public DbSet<Embedding> Embeddings { get; set; }
        public DbSet<ChatLog> ChatLogs { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<ModelQuestionPaper> ModelQuestionPapers { get; set; }
        public DbSet<EvaluationSheet> EvaluationSheets { get; set; }

        // New: SQL-backed chat history and study notes (migrated from Azure Functions)
        public DbSet<ChatHistory> ChatHistories { get; set; }
        public DbSet<StudyNote> StudyNotes { get; set; }

        // Azure Functions ingestion tables (shared schema)
        public DbSet<FileChunk> FileChunks { get; set; }
        public DbSet<ChunkEmbedding> ChunkEmbeddings { get; set; }

        // Exam System entities
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<ExamTemplate> ExamTemplates { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<ExamAnswer> ExamAnswers { get; set; }

        // Subjective rubric storage for step-based marking
        public DbSet<SubjectiveRubric> SubjectiveRubrics { get; set; }

        // Generated exams storage (persisted for MCQ/written answer submissions)
        public DbSet<GeneratedExam> GeneratedExams { get; set; }

        // Written submission tracking (shared with Azure Functions)
        public DbSet<WrittenSubmission> WrittenSubmissions { get; set; }
        public DbSet<WrittenQuestionEvaluation> WrittenQuestionEvaluations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fix multiple cascade paths for Embedding
            modelBuilder.Entity<Embedding>()
                .HasOne(e => e.SyllabusChunk)
                .WithMany()
                .HasForeignKey(e => e.SyllabusChunkId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fix multiple cascade paths for ChatLog
            modelBuilder.Entity<ChatLog>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ChatHistory relationships
            modelBuilder.Entity<ChatHistory>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.AuthenticatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatHistory>()
                .HasIndex(c => new { c.UserId, c.SessionId, c.Timestamp });

            // Add index for ChatHistory Tag
            modelBuilder.Entity<ChatHistory>()
                .HasIndex(c => c.Tag);

            // Configure StudyNote relationships
            modelBuilder.Entity<StudyNote>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.AuthenticatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudyNote>()
                .HasIndex(s => new { s.UserId, s.CreatedAt });

            // Add index for StudyNote ShareToken for fast lookup
            modelBuilder.Entity<StudyNote>()
                .HasIndex(s => s.ShareToken)
                .IsUnique();

            // Configure FileChunk relationships (Azure Functions schema)
            modelBuilder.Entity<FileChunk>()
                .HasOne(fc => fc.UploadedFile)
                .WithMany()
                .HasForeignKey(fc => fc.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FileChunk>()
                .HasIndex(fc => new { fc.FileId, fc.ChunkIndex });

            modelBuilder.Entity<FileChunk>()
                .HasIndex(fc => new { fc.Subject, fc.Grade, fc.Chapter });

            // Configure ChunkEmbedding relationships (Azure Functions schema)
            modelBuilder.Entity<ChunkEmbedding>()
                .HasOne(ce => ce.FileChunk)
                .WithMany()
                .HasForeignKey(ce => ce.ChunkId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChunkEmbedding>()
                .HasIndex(ce => ce.ChunkId)
                .IsUnique();

            // Configure Exam System relationships
            // Question -> QuestionOptions (One-to-Many)
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasIndex(q => new { q.Subject, q.Chapter, q.Difficulty });

            // ExamTemplate -> ExamAttempts (One-to-Many)
            modelBuilder.Entity<ExamTemplate>()
                .HasMany(t => t.ExamAttempts)
                .WithOne(a => a.ExamTemplate)
                .HasForeignKey(a => a.ExamTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamTemplate>()
                .HasIndex(t => new { t.Subject, t.Chapter });

            // ExamAttempt -> ExamAnswers (One-to-Many)
            modelBuilder.Entity<ExamAttempt>()
                .HasMany(a => a.ExamAnswers)
                .WithOne(ans => ans.ExamAttempt)
                .HasForeignKey(ans => ans.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamAttempt>()
                .HasIndex(a => new { a.StudentId, a.StartedAt });

            modelBuilder.Entity<ExamAttempt>()
                .HasIndex(a => a.Status);

            // ExamAnswer -> Question (Many-to-One)
            modelBuilder.Entity<ExamAnswer>()
                .HasOne(ans => ans.Question)
                .WithMany(q => q.ExamAnswers)
                .HasForeignKey(ans => ans.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAnswer>()
                .HasIndex(ans => new { ans.ExamAttemptId, ans.QuestionId });

            // Configure SubjectiveRubric indexes for fast lookup
            modelBuilder.Entity<SubjectiveRubric>()
                .HasIndex(r => new { r.ExamId, r.QuestionId })
                .IsUnique();

            modelBuilder.Entity<SubjectiveRubric>()
                .HasIndex(r => r.ExamId);

            // Configure GeneratedExam indexes for fast lookup
            modelBuilder.Entity<GeneratedExam>()
                .HasIndex(e => e.ExamId)
                .IsUnique();

            modelBuilder.Entity<GeneratedExam>()
                .HasIndex(e => new { e.Subject, e.Grade, e.Chapter });

            modelBuilder.Entity<GeneratedExam>()
                .HasIndex(e => e.CreatedAt);

            // Configure WrittenSubmission for SQL table mapping
            modelBuilder.Entity<WrittenSubmission>()
                .HasIndex(w => w.ExamId);

            modelBuilder.Entity<WrittenSubmission>()
                .HasIndex(w => w.StudentId);

            modelBuilder.Entity<WrittenSubmission>()
                .HasIndex(w => w.Status);

            modelBuilder.Entity<WrittenSubmission>()
                .HasIndex(w => new { w.ExamId, w.StudentId })
                .IsUnique();

            // Configure WrittenQuestionEvaluation relationships
            modelBuilder.Entity<WrittenQuestionEvaluation>()
                .HasOne(e => e.WrittenSubmission)
                .WithMany()
                .HasForeignKey(e => e.WrittenSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WrittenQuestionEvaluation>()
                .HasIndex(e => e.WrittenSubmissionId);
        }
    }
}