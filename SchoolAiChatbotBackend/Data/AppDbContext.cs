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
        }
    }
}