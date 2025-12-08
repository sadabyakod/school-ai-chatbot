using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for handling file storage (local disk or cloud)
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves an uploaded file and returns the file path
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string examId, string studentId);
    }

    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _uploadDirectory;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;
            // TODO: Make this configurable via appsettings.json
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "written-answers");
            
            // Ensure directory exists
            Directory.CreateDirectory(_uploadDirectory);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string examId, string studentId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!Array.Exists(allowedExtensions, ext => ext == extension))
            {
                throw new ArgumentException($"File type {extension} is not allowed", nameof(file));
            }

            // Create unique filename
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueFileName = $"{examId}_{studentId}_{timestamp}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation(
                "Saved file {FileName} to {FilePath} for exam {ExamId}, student {StudentId}",
                file.FileName,
                filePath,
                examId,
                studentId);

            return filePath;
        }
    }

    // TODO: Implement Azure Blob Storage version
    /*
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(
            string connectionString,
            string containerName,
            ILogger<AzureBlobStorageService> logger)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string examId, string studentId)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var blobName = $"written-answers/{examId}/{studentId}/{timestamp}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            _logger.LogInformation("Uploaded file to Azure Blob: {BlobName}", blobName);
            
            return blobClient.Uri.ToString();
        }
    }
    */
}
