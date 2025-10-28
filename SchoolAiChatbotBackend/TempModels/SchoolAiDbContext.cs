using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SchoolAiChatbotBackend.TempModels;

public partial class SchoolAiDbContext : DbContext
{
    public SchoolAiDbContext(DbContextOptions<SchoolAiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatLogs> ChatLogs { get; set; }

    public virtual DbSet<Embeddings> Embeddings { get; set; }

    public virtual DbSet<Faqs> Faqs { get; set; }

    public virtual DbSet<Schools> Schools { get; set; }

    public virtual DbSet<SyllabusChunks> SyllabusChunks { get; set; }

    public virtual DbSet<UploadedFiles> UploadedFiles { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatLogs>(entity =>
        {
            entity.HasIndex(e => e.SchoolId, "IX_ChatLogs_SchoolId");

            entity.HasIndex(e => e.UserId, "IX_ChatLogs_UserId");

            entity.HasOne(d => d.School).WithMany(p => p.ChatLogs).HasForeignKey(d => d.SchoolId);

            entity.HasOne(d => d.User).WithMany(p => p.ChatLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Embeddings>(entity =>
        {
            entity.HasIndex(e => e.SchoolId, "IX_Embeddings_SchoolId");

            entity.HasIndex(e => e.SyllabusChunkId, "IX_Embeddings_SyllabusChunkId");

            entity.HasOne(d => d.School).WithMany(p => p.Embeddings).HasForeignKey(d => d.SchoolId);

            entity.HasOne(d => d.SyllabusChunk).WithMany(p => p.Embeddings)
                .HasForeignKey(d => d.SyllabusChunkId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Faqs>(entity =>
        {
            entity.HasIndex(e => e.SchoolId, "IX_Faqs_SchoolId");

            entity.HasOne(d => d.School).WithMany(p => p.Faqs).HasForeignKey(d => d.SchoolId);
        });

        modelBuilder.Entity<SyllabusChunks>(entity =>
        {
            entity.HasIndex(e => e.UploadedFileId, "IX_SyllabusChunks_UploadedFileId");

            entity.Property(e => e.PineconeVectorId).HasMaxLength(100);

            entity.HasOne(d => d.UploadedFile).WithMany(p => p.SyllabusChunks)
                .HasForeignKey(d => d.UploadedFileId)
                .HasConstraintName("FK__SyllabusC__Uploa__4BAC3F29");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasIndex(e => e.SchoolId, "IX_Users_SchoolId");

            entity.HasOne(d => d.School).WithMany(p => p.Users).HasForeignKey(d => d.SchoolId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
