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
        Task<string> UploadFileToBlobAsync(string fileName, Stream fileStream, string? contentType = null);
        Task<bool> DeleteBlobAsync(string blobUrl);
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

            // Try to get connection string from Azure Functions compatible keys
            var connectionString = configuration["AzureWebJobsStorage"] 
                ?? configuration["AzureBlobStorage:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage");

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
                _logger.LogWarning("Blob Storage not configured. Set AzureWebJobsStorage in configuration.");
                _isConfigured = false;
            }
        }

        /// <summary>
        /// Upload a file to Azure Blob Storage
        /// </summary>
        public async Task<string> UploadFileToBlobAsync(string fileName, Stream fileStream, string? contentType = null)
        {
            if (!_isConfigured || _blobServiceClient == null)
            {
                _logger.LogWarning("Blob storage not configured. Skipping upload for {FileName}", fileName);
                return $"local://uploads/{fileName}"; // Return local path as fallback
            }

            try
            {
                // Get or create container
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // Generate unique blob name
                var blobName = $"{Guid.NewGuid()}_{fileName}";
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
                _logger.LogError(ex, "Error uploading file {FileName} to blob storage", fileName);
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
    }
}
