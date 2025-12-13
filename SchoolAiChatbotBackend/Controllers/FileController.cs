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
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // 500 MB
        public async Task<IActionResult> Upload()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var file = form.Files.GetFile("file");
                var medium = form["medium"].ToString();
                var className = form["className"].ToString();
                var subject = form["subject"].ToString();

                _logger.LogInformation("File upload request received. File null: {FileNull}, FileName: {FileName}, Medium: {Medium}, Class: {Class}, Subject: {Subject}",
                    file == null, file?.FileName ?? "null", medium, className, subject);

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("File upload validation failed. File is null or empty.");
                    return BadRequest(new { status = "error", message = "No file uploaded. Please select a file." });
                }

                _logger.LogInformation("Uploading file: {FileName}, Size: {Size} bytes, Medium: {Medium}, Class: {Class}, Subject: {Subject}",
                    file.FileName, file.Length, medium, className, subject);


                string blobUrl;

                // Upload to Azure Blob Storage with folder structure (keeping existing blob storage logic)
                using (var stream = file.OpenReadStream())
                {
                    blobUrl = await _blobStorageService.UploadFileToBlobAsync(
                        file.FileName,
                        stream,
                        file.ContentType,
                        className,  // grade parameter maps to className
                        subject,
                        null);      // chapter is now null (removed)
                }

                // Save metadata to UploadedFiles table (Azure Functions schema)
                var uploadedFile = new UploadedFile
                {
                    FileName = file.FileName,
                    BlobUrl = blobUrl,
                    UploadedAt = DateTime.UtcNow,
                    Medium = medium,
                    Subject = subject,
                    Grade = className,
                    UploadedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    Status = "Pending", // Azure Functions will update this to "Processing" -> "Completed"
                    TotalChunks = 0 // Will be set by Azure Functions after processing
                };

                try
                {
                    _dbContext.UploadedFiles.Add(uploadedFile);

                    _logger.LogInformation("Attempting to save file metadata to database...");
                    _logger.LogInformation("Database connection: {ConnectionString}",
                        _dbContext.Database.GetConnectionString()?.Substring(0, Math.Min(100, _dbContext.Database.GetConnectionString()?.Length ?? 0)));

                    var savedCount = await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Database SaveChanges completed. Rows affected: {RowCount}, FileId: {FileId}", savedCount, uploadedFile.Id);

                    if (savedCount == 0)
                    {
                        _logger.LogError("SaveChanges returned 0 rows affected - database insert may have failed!");
                        return StatusCode(500, new
                        {
                            status = "error",
                            message = "File uploaded to blob storage but failed to save metadata to database.",
                            blobUrl = blobUrl
                        });
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error while saving file metadata: {Message}", dbEx.InnerException?.Message ?? dbEx.Message);
                    return StatusCode(500, new
                    {
                        status = "error",
                        message = "File uploaded to blob storage but database save failed: " + (dbEx.InnerException?.Message ?? dbEx.Message),
                        blobUrl = blobUrl
                    });
                }

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
                _logger.LogError(ex, "Error uploading file");
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Failed to upload file.",
                    details = innerMessage
                });
            }
        }

        // ==========================================
        // CHUNKED UPLOAD ENDPOINTS FOR LARGE FILES
        // ==========================================

        private static readonly Dictionary<string, List<string>> _uploadChunks = new();
        private static readonly object _chunksLock = new();

        /// <summary>
        /// Upload a single chunk of a large file
        /// POST /api/file/upload-chunk
        /// </summary>
        [HttpPost("upload-chunk")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 10485760)] // 10 MB per chunk
        [RequestSizeLimit(10485760)] // 10 MB per chunk
        public async Task<IActionResult> UploadChunk()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var chunk = form.Files.GetFile("chunk");
                var uploadIdStr = form["uploadId"].ToString();
                var chunkIndexStr = form["chunkIndex"].ToString();
                var totalChunksStr = form["totalChunks"].ToString();
                var fileName = form["fileName"].ToString();

                // Validate required fields
                if (string.IsNullOrWhiteSpace(uploadIdStr) ||
                    string.IsNullOrWhiteSpace(chunkIndexStr) ||
                    string.IsNullOrWhiteSpace(totalChunksStr) ||
                    string.IsNullOrWhiteSpace(fileName))
                {
                    return BadRequest(new { status = "error", message = "Missing required fields: uploadId, chunkIndex, totalChunks, or fileName." });
                }

                if (!int.TryParse(chunkIndexStr, out int chunkIndex) || chunkIndex < 0)
                {
                    return BadRequest(new { status = "error", message = "Invalid chunkIndex. Must be a non-negative integer." });
                }

                if (!int.TryParse(totalChunksStr, out int totalChunks) || totalChunks <= 0)
                {
                    return BadRequest(new { status = "error", message = "Invalid totalChunks. Must be a positive integer." });
                }

                if (chunkIndex >= totalChunks)
                {
                    return BadRequest(new { status = "error", message = $"Invalid chunkIndex {chunkIndex}. Must be less than totalChunks {totalChunks}." });
                }

                _logger.LogInformation("Receiving chunk {ChunkIndex}/{TotalChunks} for upload {UploadId}, file: {FileName}",
                    chunkIndex + 1, totalChunks, uploadIdStr, fileName);

                if (chunk == null || chunk.Length == 0)
                {
                    return BadRequest(new { status = "error", message = "No chunk data received." });
                }

                // Validate chunk size (max 10MB)
                if (chunk.Length > 10485760)
                {
                    return BadRequest(new { status = "error", message = $"Chunk size {chunk.Length} bytes exceeds maximum of 10MB." });
                }

                // Create temp directory for chunks
                var tempDir = Path.Combine(Path.GetTempPath(), "school-ai-uploads", uploadIdStr);
                try
                {
                    Directory.CreateDirectory(tempDir);
                }
                catch (Exception dirEx)
                {
                    _logger.LogError(dirEx, "Failed to create temp directory for upload {UploadId}", uploadIdStr);
                    return StatusCode(500, new { status = "error", message = "Failed to create temporary storage directory.", details = dirEx.Message });
                }

                // Save chunk to temp file
                var chunkPath = Path.Combine(tempDir, $"chunk_{chunkIndex:D5}");

                // Check if chunk already exists (duplicate upload)
                if (System.IO.File.Exists(chunkPath))
                {
                    _logger.LogWarning("Chunk {ChunkIndex} already exists for upload {UploadId}, overwriting", chunkIndex, uploadIdStr);
                }

                try
                {
                    using (var stream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await chunk.CopyToAsync(stream);
                        await stream.FlushAsync();
                    }

                    // Verify chunk was written correctly
                    var fileInfo = new FileInfo(chunkPath);
                    if (!fileInfo.Exists || fileInfo.Length != chunk.Length)
                    {
                        _logger.LogError("Chunk verification failed for {ChunkIndex}. Expected {Expected} bytes, got {Actual} bytes",
                            chunkIndex, chunk.Length, fileInfo.Length);
                        return StatusCode(500, new { status = "error", message = "Chunk verification failed. File may be corrupted." });
                    }

                    _logger.LogInformation("Chunk {ChunkIndex}/{TotalChunks} saved successfully ({Size} bytes) for upload {UploadId}",
                        chunkIndex + 1, totalChunks, chunk.Length, uploadIdStr);
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "Failed to save chunk {ChunkIndex} for upload {UploadId}", chunkIndex, uploadIdStr);
                    return StatusCode(500, new { status = "error", message = "Failed to save chunk to disk.", details = fileEx.Message });
                }

                // Track chunks (thread-safe)
                int chunksReceived;
                lock (_chunksLock)
                {
                    if (!_uploadChunks.ContainsKey(uploadIdStr))
                    {
                        _uploadChunks[uploadIdStr] = new List<string>();
                    }
                    if (!_uploadChunks[uploadIdStr].Contains(chunkPath))
                    {
                        _uploadChunks[uploadIdStr].Add(chunkPath);
                    }
                    chunksReceived = _uploadChunks[uploadIdStr].Count;
                }

                return Ok(new
                {
                    status = "success",
                    message = $"Chunk {chunkIndex + 1} of {totalChunks} received and verified.",
                    uploadId = uploadIdStr,
                    chunkIndex,
                    totalChunks,
                    chunksReceived,
                    allChunksReceived = chunksReceived == totalChunks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading chunk");
                return StatusCode(500, new { status = "error", message = "Failed to upload chunk.", details = ex.Message });
            }
        }

        /// <summary>
        /// Finalize chunked upload - assemble chunks into final file
        /// POST /api/file/finalize-upload
        /// </summary>
        [HttpPost("finalize-upload")]
        [RequestSizeLimit(1048576)] // 1 MB for JSON request body
        public async Task<IActionResult> FinalizeUpload([FromBody] FinalizeUploadRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrWhiteSpace(request.UploadId))
                {
                    return BadRequest(new { status = "error", message = "UploadId is required." });
                }

                if (string.IsNullOrWhiteSpace(request.FileName))
                {
                    return BadRequest(new { status = "error", message = "FileName is required." });
                }

                if (request.TotalChunks <= 0)
                {
                    return BadRequest(new { status = "error", message = "TotalChunks must be greater than 0." });
                }

                if (request.FileSize <= 0)
                {
                    return BadRequest(new { status = "error", message = "FileSize must be greater than 0." });
                }

                // Validate file size (max 500MB)
                if (request.FileSize > 524288000)
                {
                    return BadRequest(new { status = "error", message = "File size exceeds maximum limit of 500MB." });
                }

                _logger.LogInformation("Finalizing upload {UploadId} for file {FileName}, {TotalChunks} chunks, {FileSize} bytes",
                    request.UploadId, request.FileName, request.TotalChunks, request.FileSize);

                var tempDir = Path.Combine(Path.GetTempPath(), "school-ai-uploads", request.UploadId);

                if (!Directory.Exists(tempDir))
                {
                    _logger.LogWarning("Upload session not found for {UploadId}", request.UploadId);
                    return BadRequest(new { status = "error", message = "Upload session not found. Chunks may have expired or were never uploaded." });
                }

                // Get all chunk files sorted by name
                var chunkFiles = Directory.GetFiles(tempDir, "chunk_*")
                    .OrderBy(f => f)
                    .ToList();

                _logger.LogInformation("Found {ChunkCount} chunk files for upload {UploadId}", chunkFiles.Count, request.UploadId);

                if (chunkFiles.Count != request.TotalChunks)
                {
                    _logger.LogError("Chunk count mismatch for upload {UploadId}. Expected {Expected}, found {Found}",
                        request.UploadId, request.TotalChunks, chunkFiles.Count);
                    return BadRequest(new
                    {
                        status = "error",
                        message = $"Missing chunks. Expected {request.TotalChunks}, found {chunkFiles.Count}.",
                        expectedChunks = request.TotalChunks,
                        receivedChunks = chunkFiles.Count
                    });
                }

                // Assemble final file
                var finalFilePath = Path.Combine(tempDir, request.FileName);
                long assembledSize = 0;

                try
                {
                    using (var finalStream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        foreach (var chunkFile in chunkFiles)
                        {
                            try
                            {
                                using var chunkStream = new FileStream(chunkFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                                await chunkStream.CopyToAsync(finalStream);
                                assembledSize += chunkStream.Length;
                            }
                            catch (Exception chunkEx)
                            {
                                _logger.LogError(chunkEx, "Failed to read chunk file {ChunkFile}", chunkFile);
                                throw new IOException($"Failed to read chunk file: {Path.GetFileName(chunkFile)}", chunkEx);
                            }
                        }
                        await finalStream.FlushAsync();
                    }

                    var finalFileInfo = new FileInfo(finalFilePath);
                    _logger.LogInformation("Assembled file {FileName}, size: {Size} bytes (expected: {Expected} bytes)",
                        request.FileName, finalFileInfo.Length, request.FileSize);

                    // Verify assembled file size matches expected size (allow 1% variance due to metadata)
                    var sizeDifference = Math.Abs(finalFileInfo.Length - request.FileSize);
                    var allowedVariance = request.FileSize * 0.01; // 1% variance

                    if (sizeDifference > allowedVariance && sizeDifference > 1024) // Allow 1KB difference or 1%
                    {
                        _logger.LogError("File size mismatch after assembly. Expected {Expected}, got {Actual}",
                            request.FileSize, finalFileInfo.Length);
                        return StatusCode(500, new
                        {
                            status = "error",
                            message = "File size verification failed. The assembled file size does not match the expected size.",
                            expectedSize = request.FileSize,
                            actualSize = finalFileInfo.Length
                        });
                    }
                }
                catch (Exception assemblyEx)
                {
                    _logger.LogError(assemblyEx, "Failed to assemble chunks for upload {UploadId}", request.UploadId);
                    return StatusCode(500, new { status = "error", message = "Failed to assemble file chunks.", details = assemblyEx.Message });
                }

                // Upload to Azure Blob Storage
                string blobUrl;
                try
                {
                    using (var fileStream = new FileStream(finalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var contentType = GetContentType(request.FileName);
                        _logger.LogInformation("Uploading assembled file {FileName} to Azure Blob Storage", request.FileName);

                        blobUrl = await _blobStorageService.UploadFileToBlobAsync(
                            request.FileName,
                            fileStream,
                            contentType,
                            request.ClassName,
                            request.Subject,
                            null);

                        _logger.LogInformation("File {FileName} uploaded to blob storage: {BlobUrl}", request.FileName, blobUrl);
                    }
                }
                catch (Exception blobEx)
                {
                    _logger.LogError(blobEx, "Failed to upload file {FileName} to blob storage", request.FileName);
                    return StatusCode(500, new { status = "error", message = "Failed to upload file to blob storage.", details = blobEx.Message });
                }

                // Save metadata to database
                var uploadedFile = new UploadedFile
                {
                    FileName = request.FileName,
                    BlobUrl = blobUrl,
                    UploadedAt = DateTime.UtcNow,
                    Medium = request.Medium ?? "",
                    Subject = request.Subject ?? "",
                    Grade = request.ClassName ?? "",
                    UploadedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "chunked-upload",
                    Status = "Pending",
                    TotalChunks = 0
                };

                try
                {
                    _dbContext.UploadedFiles.Add(uploadedFile);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("File metadata saved to database with FileId: {FileId}", uploadedFile.Id);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save file metadata to database for {FileName}", request.FileName);
                    return StatusCode(500, new { status = "error", message = "File uploaded but failed to save metadata.", details = dbEx.Message });
                }

                // Cleanup temp files
                try
                {
                    _logger.LogInformation("Cleaning up temporary files for upload {UploadId}", request.UploadId);

                    // Delete individual chunk files first
                    foreach (var chunkFile in chunkFiles)
                    {
                        try
                        {
                            if (System.IO.File.Exists(chunkFile))
                            {
                                System.IO.File.Delete(chunkFile);
                            }
                        }
                        catch (Exception chunkDelEx)
                        {
                            _logger.LogWarning(chunkDelEx, "Failed to delete chunk file {ChunkFile}", chunkFile);
                        }
                    }

                    // Delete assembled file
                    if (System.IO.File.Exists(finalFilePath))
                    {
                        System.IO.File.Delete(finalFilePath);
                    }

                    // Delete temp directory
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }

                    // Remove from tracking dictionary
                    lock (_chunksLock)
                    {
                        _uploadChunks.Remove(request.UploadId);
                    }

                    _logger.LogInformation("Cleanup completed for upload {UploadId}", request.UploadId);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup temp files for upload {UploadId}. Files will be cleaned up by system eventually.", request.UploadId);
                }

                _logger.LogInformation("Chunked upload completed successfully: {FileName}, FileId: {FileId}, Size: {Size} bytes, BlobUrl: {BlobUrl}",
                    request.FileName, uploadedFile.Id, request.FileSize, blobUrl);

                return Ok(new
                {
                    status = "success",
                    message = "File uploaded successfully. Processing will begin automatically.",
                    fileId = uploadedFile.Id,
                    fileName = request.FileName,
                    fileSize = request.FileSize,
                    totalChunks = request.TotalChunks,
                    blobUrl = blobUrl,
                    uploadedAt = uploadedFile.UploadedAt,
                    note = "Azure Functions will process this file and generate embeddings automatically."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing upload {UploadId}", request.UploadId);

                // Attempt cleanup even on error
                try
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), "school-ai-uploads", request.UploadId);
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    lock (_chunksLock)
                    {
                        _uploadChunks.Remove(request.UploadId);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup after error for upload {UploadId}", request.UploadId);
                }

                return StatusCode(500, new { status = "error", message = "Failed to finalize upload.", details = ex.Message });
            }
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };
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
                        f.Medium,
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

        // ==========================================
        // SYLLABUS DOWNLOAD ENDPOINTS
        // ==========================================

        /// <summary>
        /// Get available syllabi grouped by subject and class
        /// GET /api/file/syllabus
        /// </summary>
        [HttpGet("syllabus")]
        public async Task<IActionResult> GetAvailableSyllabus(
            [FromQuery] string? grade = null,
            [FromQuery] string? medium = null)
        {
            try
            {
                // Show all uploaded files for download (all files are syllabi for now)
                var query = _dbContext.UploadedFiles.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(grade))
                {
                    query = query.Where(f => f.Grade == grade);
                }

                if (!string.IsNullOrWhiteSpace(medium))
                {
                    query = query.Where(f => f.Medium == medium);
                }

                var syllabi = await query
                    .GroupBy(f => new { f.Subject, f.Grade, f.Medium })
                    .Select(g => new
                    {
                        Subject = g.Key.Subject ?? "General",
                        Grade = g.Key.Grade ?? "All",
                        Medium = g.Key.Medium ?? "English",
                        FileCount = g.Count(),
                        LatestUpload = g.Max(f => f.UploadedAt),
                        Files = g.Select(f => new
                        {
                            f.Id,
                            f.FileName,
                            f.BlobUrl,
                            f.UploadedAt
                        }).OrderByDescending(f => f.UploadedAt).ToList()
                    })
                    .OrderBy(s => s.Subject)
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = syllabi.Count,
                    syllabi
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available syllabi");
                return StatusCode(500, new { status = "error", message = "Failed to fetch syllabi." });
            }
        }

        /// <summary>
        /// Get syllabus list for a specific subject and class
        /// GET /api/file/syllabus/{subject}
        /// </summary>
        [HttpGet("syllabus/{subject}")]
        public async Task<IActionResult> GetSyllabusBySubject(
            string subject,
            [FromQuery] string? grade = null,
            [FromQuery] string? medium = null)
        {
            try
            {
                var query = _dbContext.UploadedFiles
                    .Where(f => f.Subject != null && 
                           f.Subject.ToLower() == subject.ToLower())
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(grade))
                {
                    query = query.Where(f => f.Grade == grade);
                }

                if (!string.IsNullOrWhiteSpace(medium))
                {
                    query = query.Where(f => f.Medium == medium);
                }

                var files = await query
                    .OrderByDescending(f => f.UploadedAt)
                    .Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.BlobUrl,
                        f.Subject,
                        f.Grade,
                        f.Medium,
                        f.UploadedAt,
                        f.TotalChunks
                    })
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    subject,
                    grade,
                    medium,
                    count = files.Count,
                    files
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching syllabus for subject: {Subject}", subject);
                return StatusCode(500, new { status = "error", message = "Failed to fetch syllabus." });
            }
        }

        /// <summary>
        /// Get available grades/classes that have uploaded syllabi
        /// GET /api/file/syllabus/grades
        /// </summary>
        [HttpGet("syllabus/grades")]
        public async Task<IActionResult> GetAvailableGrades([FromQuery] string? medium = null)
        {
            try
            {
                var query = _dbContext.UploadedFiles
                    .Where(f => f.Grade != null)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(medium))
                {
                    query = query.Where(f => f.Medium == medium);
                }

                var grades = await query
                    .GroupBy(f => f.Grade)
                    .Select(g => new
                    {
                        Grade = g.Key,
                        SubjectCount = g.Select(f => f.Subject).Distinct().Count(),
                        FileCount = g.Count()
                    })
                    .OrderBy(g => g.Grade)
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = grades.Count,
                    grades
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available grades");
                return StatusCode(500, new { status = "error", message = "Failed to fetch grades." });
            }
        }

        /// <summary>
        /// Get available subjects for a specific grade
        /// GET /api/file/syllabus/subjects
        /// </summary>
        [HttpGet("syllabus/subjects")]
        public async Task<IActionResult> GetAvailableSubjects(
            [FromQuery] string? grade = null,
            [FromQuery] string? medium = null)
        {
            try
            {
                var query = _dbContext.UploadedFiles
                    .Where(f => f.Subject != null)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(grade))
                {
                    query = query.Where(f => f.Grade == grade);
                }

                if (!string.IsNullOrWhiteSpace(medium))
                {
                    query = query.Where(f => f.Medium == medium);
                }

                var subjects = await query
                    .GroupBy(f => f.Subject)
                    .Select(g => new
                    {
                        Subject = g.Key,
                        FileCount = g.Count(),
                        LatestUpload = g.Max(f => f.UploadedAt)
                    })
                    .OrderBy(s => s.Subject)
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    grade,
                    medium,
                    count = subjects.Count,
                    subjects
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available subjects");
                return StatusCode(500, new { status = "error", message = "Failed to fetch subjects." });
            }
        }

        /// <summary>
        /// Get download URL for a specific syllabus file
        /// GET /api/file/syllabus/download/{fileId}
        /// </summary>
        [HttpGet("syllabus/download/{fileId}")]
        public async Task<IActionResult> GetSyllabusDownloadUrl(int fileId)
        {
            try
            {
                var file = await _dbContext.UploadedFiles
                    .Where(f => f.Id == fileId)
                    .FirstOrDefaultAsync();

                if (file == null)
                {
                    return NotFound(new { status = "error", message = "Syllabus file not found." });
                }

                return Ok(new
                {
                    status = "success",
                    fileId = file.Id,
                    fileName = file.FileName,
                    subject = file.Subject,
                    grade = file.Grade,
                    medium = file.Medium,
                    downloadUrl = file.BlobUrl,
                    uploadedAt = file.UploadedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting download URL for file: {FileId}", fileId);
                return StatusCode(500, new { status = "error", message = "Failed to get download URL." });
            }
        }

        // ==========================================
        // MODEL QUESTION PAPER ENDPOINTS
        // Note: These endpoints require database migration to add 
        // FileType, Chapter, and AcademicYear columns.
        // For now, returning "coming soon" response.
        // ==========================================

        /// <summary>
        /// Get all model question papers - Coming Soon
        /// GET /api/file/question-papers
        /// </summary>
        [HttpGet("question-papers")]
        public IActionResult GetAllQuestionPapers()
        {
            return Ok(new
            {
                status = "coming_soon",
                message = "Model question papers feature is coming soon. Database migration required.",
                count = 0,
                questionPapers = Array.Empty<object>()
            });
        }

        /// <summary>
        /// Get question papers by subject - Coming Soon
        /// </summary>
        [HttpGet("question-papers/{subject}")]
        public IActionResult GetQuestionPapersBySubject(string subject)
        {
            return Ok(new
            {
                status = "coming_soon",
                message = "Model question papers feature is coming soon.",
                subject,
                count = 0,
                papers = Array.Empty<object>()
            });
        }

        /// <summary>
        /// Get question papers by grade - Coming Soon
        /// </summary>
        [HttpGet("question-papers/grade/{grade}")]
        public IActionResult GetQuestionPapersByGrade(string grade)
        {
            return Ok(new
            {
                status = "coming_soon",
                message = "Model question papers feature is coming soon.",
                grade,
                subjectCount = 0,
                totalPapers = 0,
                subjects = Array.Empty<object>()
            });
        }

        /// <summary>
        /// Get available grades - Coming Soon
        /// </summary>
        [HttpGet("question-papers/grades")]
        public IActionResult GetQuestionPaperGrades()
        {
            return Ok(new
            {
                status = "coming_soon",
                message = "Model question papers feature is coming soon.",
                count = 0,
                grades = Array.Empty<object>()
            });
        }

        /// <summary>
        /// Get available subjects - Coming Soon
        /// </summary>
        [HttpGet("question-papers/subjects")]
        public IActionResult GetQuestionPaperSubjects()
        {
            return Ok(new
            {
                status = "coming_soon",
                message = "Model question papers feature is coming soon.",
                count = 0,
                subjects = Array.Empty<object>()
            });
        }

        /// <summary>
        /// Download question paper - Coming Soon
        /// </summary>
        [HttpGet("question-papers/download/{fileId}")]
        public IActionResult GetQuestionPaperDownloadUrl(int fileId)
        {
            return Ok(new
            {
                status = "coming_soon",
                message = "Model question papers feature is coming soon.",
                fileId
            });
        }

        /// <summary>
        /// Get academic years - Coming Soon
        /// </summary>
        [HttpGet("question-papers/years")]
        public IActionResult GetQuestionPaperYears()
        {
            return Ok(new
            {
                status = "coming_soon",
                message = "Model question papers feature is coming soon.",
                count = 0,
                years = Array.Empty<object>()
            });
        }
    }
}
