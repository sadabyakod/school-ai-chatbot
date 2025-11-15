using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly IBlobStorageService _blobStorageService;
        private readonly AppDbContext _dbContext;

        public FileController(
            ILogger<FileController> logger,
            IBlobStorageService blobStorageService,
            AppDbContext dbContext)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Upload a file to Azure Blob Storage and save metadata to UploadedFiles table
        /// Azure Functions blob trigger will handle chunking and embedding generation
        /// POST /api/file/upload
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file,
            [FromForm] string? subject,
            [FromForm] string? grade,
            [FromForm] string? chapter)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { status = "error", message = "No file uploaded." });

            _logger.LogInformation("Uploading file: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

            try
            {
                string blobUrl;

                // Upload to Azure Blob Storage
                using (var stream = file.OpenReadStream())
                {
                    blobUrl = await _blobStorageService.UploadFileToBlobAsync(
                        file.FileName, 
                        stream, 
                        file.ContentType);
                }

                // Save metadata to UploadedFiles table (Azure Functions schema)
                var uploadedFile = new UploadedFile
                {
                    FileName = file.FileName,
                    BlobUrl = blobUrl,
                    UploadedAt = DateTime.UtcNow,
                    Subject = subject,
                    Grade = grade,
                    Chapter = chapter,
                    UploadedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    Status = "Pending", // Azure Functions will update this to "Processing" -> "Completed"
                    TotalChunks = null // Will be set by Azure Functions after processing
                };

                _dbContext.UploadedFiles.Add(uploadedFile);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("File uploaded successfully: {FileName}, FileId: {FileId}, BlobUrl: {BlobUrl}", 
                    file.FileName, uploadedFile.Id, blobUrl);

                return Ok(new
                {
                    status = "success",
                    message = "File uploaded successfully. Processing will begin automatically.",
                    fileId = uploadedFile.Id,
                    fileName = file.FileName,
                    blobUrl = blobUrl,
                    uploadedAt = uploadedFile.UploadedAt,
                    note = "Azure Functions will process this file and generate embeddings automatically."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Failed to upload file.",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Get upload status and processing progress
        /// GET /api/file/status/{fileId}
        /// </summary>
        [HttpGet("status/{fileId}")]
        public async Task<IActionResult> GetStatus(int fileId)
        {
            try
            {
                var uploadedFile = await _dbContext.UploadedFiles.FindAsync(fileId);

                if (uploadedFile == null)
                    return NotFound(new { status = "error", message = "File not found." });

                // Check how many chunks have been created
                var chunksCreated = await _dbContext.FileChunks
                    .Where(fc => fc.FileId == fileId)
                    .CountAsync();

                var embeddingsCreated = await _dbContext.ChunkEmbeddings
                    .Where(ce => _dbContext.FileChunks
                        .Where(fc => fc.FileId == fileId)
                        .Select(fc => fc.Id)
                        .Contains(ce.ChunkId))
                    .CountAsync();

                return Ok(new
                {
                    status = "success",
                    fileId = uploadedFile.Id,
                    fileName = uploadedFile.FileName,
                    uploadedAt = uploadedFile.UploadedAt,
                    processingStatus = uploadedFile.Status,
                    chunksCreated = chunksCreated,
                    embeddingsCreated = embeddingsCreated,
                    totalChunks = uploadedFile.TotalChunks,
                    isComplete = uploadedFile.Status == "Completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file status for FileId: {FileId}", fileId);
                return StatusCode(500, new { status = "error", message = "Failed to get file status." });
            }
        }

        /// <summary>
        /// List all uploaded files
        /// GET /api/file/list
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles([FromQuery] int limit = 50)
        {
            try
            {
                var files = await _dbContext.UploadedFiles
                    .OrderByDescending(f => f.UploadedAt)
                    .Take(limit)
                    .Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.BlobUrl,
                        f.UploadedAt,
                        f.Subject,
                        f.Grade,
                        f.Chapter,
                        f.Status,
                        f.TotalChunks
                    })
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = files.Count(),
                    files
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                return StatusCode(500, new { status = "error", message = "Failed to list files." });
            }
        }
    }
}
