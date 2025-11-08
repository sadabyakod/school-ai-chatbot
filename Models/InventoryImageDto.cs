namespace ImageAPI.Models
{
    public class InventoryImageDto
    {
        public long? ID { get; set; }
        public string ItemNum { get; set; } = string.Empty;
        public string Store_ID { get; set; } = string.Empty;
        public int? Position { get; set; }
        public string ImageLocation { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public bool FileExists { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }

    public class InventoryImageWithBase64
    {
        public long? ID { get; set; }
        public string ItemNum { get; set; } = string.Empty;
        public string Store_ID { get; set; } = string.Empty;
        public int? Position { get; set; }
        public string ImageLocation { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public bool FileExists { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Base64Data { get; set; }
    }
}