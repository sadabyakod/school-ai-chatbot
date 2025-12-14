using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Azure Blob Storage Service for file uploads
    /// Compatible with Azure Functions AzureWebJobsStorage configuration
    /// </summary>
    public interface IBlobStorageService
    {
        Task<string> UploadFileToBlobAsync(string fileName, Stream fileStream, string? contentType = null, string? grade = null, string? subject = null, string? chapter = null);
        Task<string> UploadWithCustomPathAsync(string fileName, Stream fileStream, string folderPath, string? contentType = null, string? containerName = null);
        Task<bool> DeleteBlobAsync(string blobUrl);
        Task<bool> DeleteAsync(string blobName, string? containerName = null);
        Task<List<BlobInfo>> ListBlobsAsync(string? prefix = null, string? containerName = null);
        Task<Stream?> DownloadBlobAsync(string blobName);
        Task<Stream?> DownloadAsync(string blobName, string? containerName = null);
        bool IsConfigured { get; }
    }

    public class BlobInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient? _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly string _containerName = "textbooks";
        private readonly bool _isConfigured;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _logger = logger;

            // Try to get connection string from configuration
            var connectionString = configuration["BlobStorage:ConnectionString"]
                ?? configuration["AzureWebJobsStorage"]
                ?? configuration["AzureBlobStorage:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Also get container name from configuration
            _containerName = configuration["BlobStorage:ContainerName"] ?? "textbooks";

            if (!string.IsNullOrWhiteSpace(connectionString) &&
                !connectionString.Equals("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _blobServiceClient = new BlobServiceClient(connectionString);
                    _isConfigured = true;
                    _logger.LogInformation("Blob Storage Service initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Blob Storage Service");
                    _isConfigured = false;
                }
            }
            else
            {
                _logger.LogWarning("Blob Storage not configured. Set BlobStorage:ConnectionString in appsettings.json.");
                _isConfigured = false;
            }
        }

        /// <summary>
        /// Upload a file to Azure Blob Storage with folder structure: grade/subject/chapter/filename
        /// </summary>
        public async Task<string> UploadFileToBlobAsync(string fileName, Stream fileStream, string? contentType = null, string? grade = null, string? subject = null, string? chapter = null)
        {
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Skipping upload for {FileName}", fileName);
                return $"local://uploads/{fileName}"; // Return local path as fallback
            }

            try
            {
                // Get or create container (without public access)
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                // Build folder structure: grade/subject/chapter/
                var folderPath = "";
                if (!string.IsNullOrWhiteSpace(grade))
                {
                    folderPath += $"{grade}/";
                    if (!string.IsNullOrWhiteSpace(subject))
                    {
                        folderPath += $"{subject}/";
                        if (!string.IsNullOrWhiteSpace(chapter))
                        {
                            folderPath += $"{chapter}/";
                        }
                    }
                }

                // Generate unique blob name with folder structure
                var blobName = $"{folderPath}{Guid.NewGuid()}_{fileName}";
                var blobClient = containerClient.GetBlobClient(blobName);

                // Upload file
                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType ?? "application/octet-stream"
                    }
                };

                await blobClient.UploadAsync(fileStream, uploadOptions);

                var blobUrl = blobClient.Uri.ToString();
                _logger.LogInformation("Successfully uploaded {FileName} to blob storage: {BlobUrl}", fileName, blobUrl);

                return blobUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to blob storage. Exception: {ExceptionMessage}\nStackTrace: {StackTrace}", fileName, ex.Message, ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Upload a file to Azure Blob Storage with a custom folder path and optional container
        /// Folder structure: {folderPath}/{uniqueId}_{filename}
        /// Example: model-questions container -> Karnataka/Class12/Physics/{guid}_2023_Model.pdf
        /// </summary>
        public async Task<string> UploadWithCustomPathAsync(string fileName, Stream fileStream, string folderPath, string? contentType = null, string? containerName = null)
        {
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Skipping upload for {FileName}", fileName);
                return $"local://uploads/{folderPath}/{fileName}";
            }

            try
            {
                // Use specified container or default to textbooks
                var targetContainer = containerName ?? _containerName;
                var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainer);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                // Normalize folder path (remove leading/trailing slashes)
                folderPath = folderPath.Trim('/');
                
                // Generate unique blob name with custom folder structure
                var blobName = $"{folderPath}/{Guid.NewGuid()}_{fileName}";
                var blobClient = containerClient.GetBlobClient(blobName);

                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType ?? "application/octet-stream"
                    }
                };

                await blobClient.UploadAsync(fileStream, uploadOptions);

                var blobUrl = blobClient.Uri.ToString();
                _logger.LogInformation("Successfully uploaded {FileName} to custom path {FolderPath}: {BlobUrl}", 
                    fileName, folderPath, blobUrl);

                return blobUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to custom path {FolderPath}", fileName, folderPath);
                throw;
            }
        }

        /// <summary>
        /// Delete a blob from storage
        /// </summary>
        public async Task<bool> DeleteBlobAsync(string blobUrl)
        {
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Cannot delete blob: {BlobUrl}", blobUrl);
                return false;
            }

            try
            {
                var uri = new Uri(blobUrl);
                var blobName = Path.GetFileName(uri.LocalPath);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DeleteIfExistsAsync();

                if (response.Value)
                {
                    _logger.LogInformation("Successfully deleted blob: {BlobUrl}", blobUrl);
                }

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob: {BlobUrl}", blobUrl);
                return false;
            }
        }

        /// <summary>
        /// List all blobs in the container with optional prefix filter
        /// </summary>
        public async Task<List<BlobInfo>> ListBlobsAsync(string? prefix = null, string? containerName = null)
        {
            var blobs = new List<BlobInfo>();
            
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Cannot list blobs.");
                return blobs;
            }

            try
            {
                var targetContainer = containerName ?? _containerName;
                var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainer);
                
                await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    blobs.Add(new BlobInfo
                    {
                        Name = blobItem.Name,
                        Url = blobClient.Uri.ToString(),
                        Size = blobItem.Properties.ContentLength ?? 0,
                        LastModified = blobItem.Properties.LastModified
                    });
                }
                
                _logger.LogInformation("Listed {Count} blobs from container '{Container}' with prefix '{Prefix}'", 
                    blobs.Count, targetContainer, prefix ?? "(all)");
                return blobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing blobs with prefix: {Prefix}", prefix);
                return blobs;
            }
        }

        /// <summary>
        /// Download a blob as a stream
        /// </summary>
        public async Task<Stream?> DownloadBlobAsync(string blobName)
        {
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Cannot download blob: {BlobName}", blobName);
                return null;
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                
                var response = await blobClient.DownloadStreamingAsync();
                _logger.LogInformation("Downloaded blob: {BlobName}", blobName);
                
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob: {BlobName}", blobName);
                return null;
            }
        }

        /// <summary>
        /// Download a blob from a specific container
        /// </summary>
        public async Task<Stream?> DownloadAsync(string blobName, string? containerName = null)
        {
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Cannot download blob: {BlobName}", blobName);
                return null;
            }

            try
            {
                var targetContainer = containerName ?? _containerName;
                var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainer);
                var blobClient = containerClient.GetBlobClient(blobName);
                
                var response = await blobClient.DownloadStreamingAsync();
                _logger.LogInformation("Downloaded blob: {BlobName} from container: {Container}", blobName, targetContainer);
                
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob: {BlobName} from container: {Container}", blobName, containerName);
                return null;
            }
        }

        /// <summary>
        /// Delete a blob from a specific container
        /// </summary>
        public async Task<bool> DeleteAsync(string blobName, string? containerName = null)
        {
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Cannot delete blob: {BlobName}", blobName);
                return false;
            }

            try
            {
                var targetContainer = containerName ?? _containerName;
                var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainer);
                var blobClient = containerClient.GetBlobClient(blobName);
                
                var response = await blobClient.DeleteIfExistsAsync();

                if (response.Value)
                {
                    _logger.LogInformation("Successfully deleted blob: {BlobName} from container: {Container}", blobName, targetContainer);
                }

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob: {BlobName} from container: {Container}", blobName, containerName);
                return false;
            }
        }

        /// <summary>
        /// Check if blob storage is configured
        /// </summary>
        public bool IsConfigured => _isConfigured;
    }
}
