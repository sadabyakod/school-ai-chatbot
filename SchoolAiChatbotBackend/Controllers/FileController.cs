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
        private readonly PineconeService _pineconeService;
        private readonly IChatService _chatService;
        private readonly AppDbContext _dbContext;

        public FileController(
            ILogger<FileController> logger,
            PineconeService pineconeService,
            IChatService chatService,
            AppDbContext dbContext)
        {
            _logger = logger;
            _pineconeService = pineconeService;
            _chatService = chatService;
            _dbContext = dbContext;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file,
            [FromForm] string Class,
            [FromForm] string subject,
            [FromForm] string chapter)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (string.IsNullOrWhiteSpace(Class) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(chapter))
                return BadRequest("All fields (Class, Subject, Chapter) are required.");

            var filePath = Path.Combine("Uploads", file.FileName);
            Directory.CreateDirectory("Uploads");

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string? pineconeResult = null;

            try
            {
                var rawBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                bool looksBinary = rawBytes.Any(b => b == 0);
                string fileContent = looksBinary
                    ? file.FileName
                    : System.Text.Encoding.UTF8.GetString(rawBytes);

                const int chunkSize = 1024;
                var textChunks = new List<string>();

                for (int i = 0; i < fileContent.Length; i += chunkSize)
                {
                    int length = Math.Min(chunkSize, fileContent.Length - i);
                    textChunks.Add(fileContent.Substring(i, length));
                }

                var pineconeVectors = new List<PineconeVector>();
                int chunkIndex = 0;

                foreach (var chunk in textChunks)
                {
                    var embedding = await _chatService.GetEmbeddingAsync(chunk);
                    bool hasNonZero = embedding != null && embedding.Any(v => Math.Abs(v) > 1e-9f);

                    if (!hasNonZero)
                    {
                        _logger.LogWarning("Embedding generation failed for chunk {ChunkIndex} of file {FileName}", chunkIndex, file.FileName);
                        continue;
                    }

                    var original = (embedding ?? new List<float>()).Select(x => (float)x).ToList();
                    var vectorValues = original.Count > 1024
                        ? original.Take(1024).ToList()
                        : original.Concat(Enumerable.Repeat(0f, 1024 - original.Count)).ToList();

                    var pineconeVector = new PineconeVector
                    {
                        Id = $"{Guid.NewGuid()}_{chunkIndex}",
                        Values = vectorValues,
                        Metadata = new Dictionary<string, object>
                        {
                            { "Class", Class },
                            { "fileName", file.FileName },
                            { "subject", subject },
                            { "chapter", chapter },
                            { "chunkIndex", chunkIndex },
                            { "chunkText", chunk.Substring(0, Math.Min(100, chunk.Length)) + (chunk.Length > 100 ? "..." : "") }
                        }
                    };

                    pineconeVectors.Add(pineconeVector);
                    chunkIndex++;
                }

                if (pineconeVectors.Count == 0)
                    return StatusCode(500, new { message = "No valid embeddings could be generated for this file." });

                var upsertRequest = new PineconeUpsertRequest { Vectors = pineconeVectors };
                var (ok, result) = await _pineconeService.UpsertVectorsAsync(upsertRequest);
                pineconeResult = result;

                if (!ok)
                    return StatusCode(500, new { message = "File uploaded but vector upsert failed.", details = result });

                // ✅ Transaction-safe retry block (Pomelo + EF Core 9)
                var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
                await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                    try
                    {
                        var uploadedFile = new UploadedFile
                        {
                            FileName = file.FileName,
                            FilePath = filePath,
                            UploadDate = DateTime.UtcNow,
                            EmbeddingDimension = 1024,
                            EmbeddingVector = $"MultipleChunks_{pineconeVectors.Count}"
                        };

                        _logger.LogInformation("Creating UploadedFile record: {FileName}, Size: {Size}", file.FileName, fileContent.Length);
                        _dbContext.UploadedFiles.Add(uploadedFile);
                        await _dbContext.SaveChangesAsync();

                        int syllabusChunksCreated = 0;
                        foreach (var pineconeVector in pineconeVectors)
                        {
                            var chunkText = pineconeVector.Metadata.TryGetValue("chunkText", out var text)
                                ? text?.ToString()
                                : "Text chunk content";

                            var syllabusChunk = new SyllabusChunk
                            {
                                Subject = subject,
                                Grade = Class,
                                Chapter = chapter,
                                ChunkText = chunkText,
                                Source = file.FileName,
                                UploadedFileId = uploadedFile.Id,
                                PineconeVectorId = pineconeVector.Id
                            };

                            _dbContext.SyllabusChunks.Add(syllabusChunk);
                            syllabusChunksCreated++;
                        }

                        _logger.LogInformation("Saving {Count} syllabus chunks to DB", syllabusChunksCreated);
                        await _dbContext.SaveChangesAsync();

                        await transaction.CommitAsync();
                        _logger.LogInformation("Successfully created {Count} syllabus chunks for file {File}", syllabusChunksCreated, file.FileName);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return Ok(new
                {
                    message = "File uploaded successfully and processed in chunks.",
                    fileName = file.FileName,
                    chunksProcessed = pineconeVectors.Count,
                    totalFileSize = fileContent.Length,
                    pineconeResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload failed for {File}", file.FileName);
                return StatusCode(500, new
                {
                    message = "Server error during upload or vectorization.",
                    details = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
    }
}
