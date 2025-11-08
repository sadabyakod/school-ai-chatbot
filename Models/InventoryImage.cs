using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImageAPI.Models
{
    [Table("Inventory_Image")]
    public class InventoryImage
    {
        public long? ID { get; set; }

        [Required]
        [StringLength(20)]
        public string ItemNum { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Store_ID { get; set; } = string.Empty;

        public int? Position { get; set; }

        [Required]
        [StringLength(4000)]
        public string ImageLocation { get; set; } = string.Empty;

        // Computed property to check if file exists
        [NotMapped]
        public bool FileExists => !string.IsNullOrEmpty(ImageLocation) && File.Exists(ImageLocation);

        // Computed property to get file info
        [NotMapped]
        public string? ContentType
        {
            get
            {
                if (string.IsNullOrEmpty(ImageLocation)) return null;
                
                var extension = Path.GetExtension(ImageLocation).ToLowerInvariant();
                return extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    ".tiff" or ".tif" => "image/tiff",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };
            }
        }

        [NotMapped]
        public long? FileSize
        {
            get
            {
                if (!FileExists) return null;
                try
                {
                    var fileInfo = new FileInfo(ImageLocation);
                    return fileInfo.Length;
                }
                catch
                {
                    return null;
                }
            }
        }

        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(ItemNum) 
            ? $"{ItemNum} - Store {Store_ID} (Pos: {Position})" 
            : "Unknown Item";
    }
}