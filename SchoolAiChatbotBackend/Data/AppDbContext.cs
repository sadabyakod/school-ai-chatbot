using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Models;

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
        
        // New: SQL-backed chat history and study notes (migrated from Azure Functions)
        public DbSet<ChatHistory> ChatHistories { get; set; }
        public DbSet<StudyNote> StudyNotes { get; set; }
        
        // Azure Functions ingestion tables (shared schema)
        public DbSet<FileChunk> FileChunks { get; set; }
        public DbSet<ChunkEmbedding> ChunkEmbeddings { get; set; }

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
            
            // Configure StudyNote relationships
            modelBuilder.Entity<StudyNote>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.AuthenticatedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<StudyNote>()
                .HasIndex(s => new { s.UserId, s.CreatedAt });
            
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
        }
    }
}