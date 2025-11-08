using Microsoft.EntityFrameworkCore;
using ImageAPI.Models;

namespace ImageAPI.Data
{
    public class ImageDbContext : DbContext
    {
        public ImageDbContext(DbContextOptions<ImageDbContext> options) : base(options)
        {
        }

        public DbSet<ImageRecord> Images { get; set; }
        public DbSet<InventoryImage> InventoryImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ImageRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.ImagePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ImageName).HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ContentType).HasMaxLength(50);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");

                // Index for better performance
                entity.HasIndex(e => e.ImageName);
                entity.HasIndex(e => e.CreatedDate);
            });

            modelBuilder.Entity<InventoryImage>(entity =>
            {
                // Composite primary key as defined in your table
                entity.HasKey(e => new { e.ItemNum, e.Store_ID, e.ImageLocation });
                
                entity.Property(e => e.ItemNum).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Store_ID).IsRequired().HasMaxLength(10);
                entity.Property(e => e.ImageLocation).IsRequired().HasMaxLength(4000);
                
                // Indexes for better performance
                entity.HasIndex(e => e.ItemNum);
                entity.HasIndex(e => e.Store_ID);
                entity.HasIndex(e => new { e.ItemNum, e.Store_ID });
            });

            // Seed some sample data for testing
            modelBuilder.Entity<ImageRecord>().HasData(
                new ImageRecord
                {
                    Id = 1,
                    ImagePath = @"C:\temp\sample1.jpg",
                    ImageName = "Sample Image 1",
                    Description = "A sample image for testing",
                    CreatedDate = DateTime.UtcNow,
                    ContentType = "image/jpeg",
                    FileSize = 1024000
                },
                new ImageRecord
                {
                    Id = 2,
                    ImagePath = @"C:\temp\sample2.png",
                    ImageName = "Sample Image 2",
                    Description = "Another sample image for testing",
                    CreatedDate = DateTime.UtcNow,
                    ContentType = "image/png",
                    FileSize = 2048000
                }
            );

            // Seed some sample inventory image data for testing
            modelBuilder.Entity<InventoryImage>().HasData(
                new InventoryImage
                {
                    ID = 1,
                    ItemNum = "ITEM001",
                    Store_ID = "ST001",
                    Position = 1,
                    ImageLocation = @"D:\school-ai-chatbot\winform_project\TestImages\test-red.png"
                },
                new InventoryImage
                {
                    ID = 2,
                    ItemNum = "ITEM001",
                    Store_ID = "ST001",
                    Position = 2,
                    ImageLocation = @"D:\school-ai-chatbot\winform_project\TestImages\test-blue.png"
                },
                new InventoryImage
                {
                    ID = 3,
                    ItemNum = "ITEM002",
                    Store_ID = "ST002",
                    Position = 1,
                    ImageLocation = @"D:\school-ai-chatbot\winform_project\TestImages\test-gradient.jpg"
                }
            );
        }
    }
}