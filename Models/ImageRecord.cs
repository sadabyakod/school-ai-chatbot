using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImageAPI.Models
{
    public class ImageRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ImageName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(50)]
        public string? ContentType { get; set; }

        public long? FileSize { get; set; }

        // Computed property to check if file exists
        [NotMapped]
        public bool FileExists => !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath);
    }
}