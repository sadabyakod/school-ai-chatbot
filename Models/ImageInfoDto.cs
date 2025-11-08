namespace ImageAPI.Models
{
    public class ImageInfoDto
    {
        public int Id { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public bool FileExists { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}