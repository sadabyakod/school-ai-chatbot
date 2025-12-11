using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Configuration for file storage retention policies
    /// </summary>
    public class FileStorageOptions
    {
        /// <summary>
        /// Whether to delete files immediately after successful processing
        /// </summary>
        public bool DeleteAfterProcessing { get; set; } = false;

        /// <summary>
        /// Whether to keep files only when OCR confidence is low
        /// </summary>
        public bool OnDemandStorage { get; set; } = false;

        /// <summary>
        /// Retention period in days (0 = keep forever, >0 = delete after X days)
        /// </summary>
        public int RetentionDays { get; set; } = 90;

        /// <summary>
        /// Whether to move files to Archive tier after retention period instead of deleting
        /// </summary>
        public bool ArchiveAfterRetention { get; set; } = false;
    }

    /// <summary>
    /// Service for handling file storage (local disk or cloud)
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves an uploaded file and returns the file path/URL
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string examId, string studentId);

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Downloads a file content from storage
        /// </summary>
        Task<Stream> GetFileAsync(string filePath);

        /// <summary>
        /// Downloads a file to a local path
        /// </summary>
        Task DownloadFileAsync(string sourceUrl, string destinationPath);

        /// <summary>
        /// Checks if a file exists in storage
        /// </summary>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Moves file to Archive tier (cold storage)
        /// </summary>
        Task<bool> ArchiveFileAsync(string filePath);
    }

    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _uploadDirectory;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "written-answers");
            Directory.CreateDirectory(_uploadDirectory);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string examId, string studentId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!Array.Exists(allowedExtensions, ext => ext == extension))
            {
                throw new ArgumentException($"File type {extension} is not allowed", nameof(file));
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueFileName = $"{examId}_{studentId}_{timestamp}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

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

        public Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted local file: {FilePath}", filePath);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting local file: {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        public Task<Stream> GetFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            return Task.FromResult<Stream>(File.OpenRead(filePath));
        }

        public async Task DownloadFileAsync(string sourceUrl, string destinationPath)
        {
            // For local storage, sourceUrl is just a file path
            if (File.Exists(sourceUrl))
            {
                File.Copy(sourceUrl, destinationPath, overwrite: true);
            }
            else
            {
                throw new FileNotFoundException($"Source file not found: {sourceUrl}");
            }
            await Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string filePath)
        {
            return Task.FromResult(File.Exists(filePath));
        }

        public Task<bool> ArchiveFileAsync(string filePath)
        {
            _logger.LogWarning("Archive not supported for local storage: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Azure Blob Storage implementation with process-then-delete, retention policies, and archive support
    /// </summary>
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly FileStorageOptions _options;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(
            string connectionString,
            string containerName,
            FileStorageOptions options,
            ILogger<AzureBlobStorageService> logger)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
            _options = options;
            _logger = logger;

            _logger.LogInformation(
                "Azure Blob Storage initialized - DeleteAfterProcessing: {Delete}, OnDemandStorage: {OnDemand}, RetentionDays: {Retention}, Archive: {Archive}",
                _options.DeleteAfterProcessing,
                _options.OnDemandStorage,
                _options.RetentionDays,
                _options.ArchiveAfterRetention);
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

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Create unique blob name with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueFileName = $"{timestamp}_{Guid.NewGuid():N}{extension}";
            var blobName = $"written-answers/{examId}/{studentId}/{uniqueFileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set metadata including upload timestamp for retention management
            var metadata = new Dictionary<string, string>
            {
                { "examId", examId },
                { "studentId", studentId },
                { "uploadedAt", DateTime.UtcNow.ToString("o") },
                { "originalFileName", file.FileName },
                { "retentionDays", _options.RetentionDays.ToString() }
            };

            // Upload with metadata
            using var stream = file.OpenReadStream();
            var uploadOptions = new BlobUploadOptions
            {
                Metadata = metadata,
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            };

            await blobClient.UploadAsync(stream, uploadOptions);

            _logger.LogInformation(
                "Uploaded file to Azure Blob: {BlobName} (Size: {Size} bytes, RetentionDays: {Retention})",
                blobName,
                file.Length,
                _options.RetentionDays);

            // Return blob URL
            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var blobName = blobClient.Name;
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobToDelete = containerClient.GetBlobClient(blobName);

                var response = await blobToDelete.DeleteIfExistsAsync();
                
                if (response.Value)
                {
                    _logger.LogInformation("Deleted blob from Azure Storage: {BlobName}", blobName);
                    return true;
                }
                
                _logger.LogWarning("Blob not found for deletion: {BlobName}", blobName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob: {BlobUrl}", blobUrl);
                return false;
            }
        }

        public async Task<Stream> GetFileAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var blobName = blobClient.Name;
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobToDownload = containerClient.GetBlobClient(blobName);

                var response = await blobToDownload.DownloadAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob: {BlobUrl}", blobUrl);
                throw;
            }
        }

        public async Task DownloadFileAsync(string blobUrl, string destinationPath)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var blobName = blobClient.Name;
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobToDownload = containerClient.GetBlobClient(blobName);

                _logger.LogInformation("Downloading blob {BlobName} to {DestinationPath}", blobName, destinationPath);
                
                await blobToDownload.DownloadToAsync(destinationPath);
                
                _logger.LogInformation("Successfully downloaded blob to {DestinationPath}", destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob {BlobUrl} to {DestinationPath}", blobUrl, destinationPath);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var blobName = blobClient.Name;
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobToCheck = containerClient.GetBlobClient(blobName);

                return await blobToCheck.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking blob existence: {BlobUrl}", blobUrl);
                return false;
            }
        }

        public async Task<bool> ArchiveFileAsync(string blobUrl)
        {
            try
            {
                var blobClient = new BlobClient(new Uri(blobUrl));
                var blobName = blobClient.Name;
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobToArchive = containerClient.GetBlobClient(blobName);

                // Move to Archive tier (cold storage - cheapest, but takes hours to retrieve)
                await blobToArchive.SetAccessTierAsync(AccessTier.Archive);

                _logger.LogInformation("Moved blob to Archive tier: {BlobName}", blobName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving blob: {BlobUrl}", blobUrl);
                return false;
            }
        }

        /// <summary>
        /// Cleanup expired files based on retention policy (call this periodically via background job)
        /// </summary>
        public async Task CleanupExpiredFilesAsync()
        {
            try
            {
                if (_options.RetentionDays <= 0)
                {
                    _logger.LogInformation("Retention policy disabled (RetentionDays = 0), skipping cleanup");
                    return;
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);
                
                int deletedCount = 0;
                int archivedCount = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.Metadata, prefix: "written-answers/"))
                {
                    try
                    {
                        // Check upload timestamp from metadata
                        if (blobItem.Metadata.TryGetValue("uploadedAt", out var uploadedAtStr))
                        {
                            if (DateTime.TryParse(uploadedAtStr, out var uploadedAt))
                            {
                                if (uploadedAt < cutoffDate)
                                {
                                    var blobClient = containerClient.GetBlobClient(blobItem.Name);

                                    if (_options.ArchiveAfterRetention)
                                    {
                                        // Archive instead of delete
                                        await blobClient.SetAccessTierAsync(AccessTier.Archive);
                                        archivedCount++;
                                        _logger.LogInformation("Archived expired blob: {BlobName} (Uploaded: {UploadedAt})", blobItem.Name, uploadedAt);
                                    }
                                    else
                                    {
                                        // Delete permanently
                                        await blobClient.DeleteAsync();
                                        deletedCount++;
                                        _logger.LogInformation("Deleted expired blob: {BlobName} (Uploaded: {UploadedAt})", blobItem.Name, uploadedAt);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing blob for cleanup: {BlobName}", blobItem.Name);
                    }
                }

                _logger.LogInformation(
                    "Blob cleanup completed - Deleted: {DeletedCount}, Archived: {ArchivedCount}, Retention: {RetentionDays} days",
                    deletedCount,
                    archivedCount,
                    _options.RetentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during blob cleanup");
            }
        }
    }
}
