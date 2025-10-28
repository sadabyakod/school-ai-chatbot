using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ILogger<FileController> _logger;
        private readonly SchoolAiChatbotBackend.Services.PineconeService _pineconeService;
        private readonly SchoolAiChatbotBackend.Services.IChatService _chatService;
        private readonly SchoolAiChatbotBackend.Data.AppDbContext _dbContext;

        public FileController(ILogger<FileController> logger, SchoolAiChatbotBackend.Services.PineconeService pineconeService, SchoolAiChatbotBackend.Services.IChatService chatService, SchoolAiChatbotBackend.Data.AppDbContext dbContext)
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

            // Placeholder: Save file to disk (replace with cloud storage in production)
            var filePath = Path.Combine("Uploads", file.FileName);
            Directory.CreateDirectory("Uploads");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string? pineconeResult = null;
            try
            {
                var rawBytes = System.IO.File.ReadAllBytes(filePath);
                bool looksBinary = rawBytes.Any(b => b == 0);
                string fileContent = looksBinary ? file.FileName : System.Text.Encoding.UTF8.GetString(rawBytes);

                const int chunkSize = 1024; // Maximum chunk size in characters
                var textChunks = new List<string>();
                
                // Split file content into chunks if larger than 1024 characters
                if (fileContent.Length > chunkSize)
                {
                    for (int i = 0; i < fileContent.Length; i += chunkSize)
                    {
                        int length = Math.Min(chunkSize, fileContent.Length - i);
                        textChunks.Add(fileContent.Substring(i, length));
                    }
                }
                else
                {
                    textChunks.Add(fileContent);
                }

                var pineconeVectors = new List<SchoolAiChatbotBackend.Models.PineconeVector>();
                var chunkIndex = 0;
                
                // Process each chunk and create embeddings
                foreach (var chunk in textChunks)
                {
                    var embedding = await _chatService.GetEmbeddingAsync(string.IsNullOrWhiteSpace(chunk) ? $"{file.FileName}_chunk_{chunkIndex}" : chunk);

                    bool hasNonZero = embedding != null && embedding.Any(v => System.Math.Abs(v) > 1e-9f);
                    if (!hasNonZero)
                    {
                        embedding = await _chatService.GetEmbeddingAsync(string.IsNullOrWhiteSpace(chunk) ? $"{file.FileName}_chunk_{chunkIndex}" : chunk);
                        hasNonZero = embedding != null && embedding.Any(v => System.Math.Abs(v) > 1e-9f);
                    }

                    if (!hasNonZero)
                    {
                        _logger.LogWarning("Embedding generation failed for chunk {ChunkIndex} of file {FileName}", chunkIndex, file.FileName);
                        continue; // Skip this chunk but continue with others
                    }

                    var original = (embedding ?? new System.Collections.Generic.List<float>()).Select(x => (float)x).ToList();
                    List<float> vectorValues;
                    if (original.Count > 1024)
                        vectorValues = original.Take(1024).ToList();
                    else
                    {
                        vectorValues = new List<float>(original);
                        vectorValues.AddRange(Enumerable.Repeat(0f, 1024 - original.Count));
                    }

                    var pineconeVector = new SchoolAiChatbotBackend.Models.PineconeVector
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
                            { "chunkText", chunk.Substring(0, Math.Min(100, chunk.Length)) + (chunk.Length > 100 ? "..." : "") } // Store first 100 chars as preview
                        }
                    };

                    pineconeVectors.Add(pineconeVector);
                    chunkIndex++;
                }

                if (pineconeVectors.Count == 0)
                {
                    return StatusCode(500, new { message = "No valid embeddings could be generated for any chunks of the file." });
                }

                // Upsert all vectors to Pinecone
                var upsertRequest = new SchoolAiChatbotBackend.Models.PineconeUpsertRequest
                {
                    Vectors = pineconeVectors
                };

                var (ok, result) = await _pineconeService.UpsertVectorsAsync(upsertRequest);
                pineconeResult = result;

                if (!ok)
                {
                    return StatusCode(500, new { message = "File uploaded but vector upsert failed.", details = result });
                }

                try
                {
                    // Check if we're using in-memory database (doesn't support transactions)
                    bool isInMemory = _dbContext.Database.IsInMemory();
                    
                    // Use database transaction only if not using in-memory database
                    using var transaction = isInMemory ? null : await _dbContext.Database.BeginTransactionAsync();
                    
                    try
                    {
                        // Create main uploaded file record
                        var uploadedFile = new SchoolAiChatbotBackend.Data.UploadedFile
                        {
                            FileName = file.FileName,
                            FilePath = filePath,
                            UploadDate = DateTime.UtcNow,
                            EmbeddingDimension = 1024, // Standard dimension
                            EmbeddingVector = $"MultipleChunks_{pineconeVectors.Count}"
                        };
                        
                        _logger.LogInformation("Creating UploadedFile record: {FileName}, Size: {FileSize}", file.FileName, fileContent.Length);
                        _dbContext.UploadedFiles.Add(uploadedFile);
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("UploadedFile saved with Id: {FileId}", uploadedFile.Id);

                        // Insert records into SyllabusChunks table for each processed chunk
                        var syllabusChunksCreated = 0;
                        foreach (var pineconeVector in pineconeVectors)
                        {
                            var chunkText = pineconeVector.Metadata.ContainsKey("chunkText") 
                                ? pineconeVector.Metadata["chunkText"].ToString() 
                                : "Text chunk content";
                                
                            var syllabusChunk = new SyllabusChunk
                            {
                                Subject = subject,
                                Grade = Class,
                                Chapter = chapter,
                                ChunkText = chunkText,
                                Source = file.FileName, // Set the source to the filename
                                UploadedFileId = uploadedFile.Id,
                                PineconeVectorId = pineconeVector.Id
                            };
                            _logger.LogInformation("Creating SyllabusChunk {Index}: Subject={Subject}, Grade={Grade}, Chapter={Chapter}, UploadedFileId={FileId}", 
                                syllabusChunksCreated, subject, Class, chapter, uploadedFile.Id);
                            _dbContext.SyllabusChunks.Add(syllabusChunk);
                            syllabusChunksCreated++;
                        }
                        
                        _logger.LogInformation("Saving {ChunkCount} SyllabusChunk records to database", syllabusChunksCreated);
                        await _dbContext.SaveChangesAsync();
                        
                        // Commit the transaction if both operations succeeded (only for real databases)
                        if (transaction != null)
                        {
                            await transaction.CommitAsync();
                        }
                        
                        _logger.LogInformation("Successfully created {ChunkCount} syllabus chunks for file {FileName}", 
                            syllabusChunksCreated, file.FileName);
                    }
                    catch (Exception dbEx)
                    {
                        // Rollback transaction on any error (only for real databases)
                        if (transaction != null)
                        {
                            await transaction.RollbackAsync();
                        }
                        throw; // Re-throw to be caught by outer catch block
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save UploadedFile or SyllabusChunks to DB for {File}. Error: {ErrorMessage}", file.FileName, ex.Message);
                    
                    // Log inner exception details for debugging
                    var innerException = ex.InnerException;
                    var innerMessage = innerException?.Message ?? "No inner exception";
                    _logger.LogError("Inner exception: {InnerMessage}", innerMessage);
                    
                    // Get full exception details including stack trace
                    var fullErrorDetails = $"Main: {ex.Message}. Inner: {innerMessage}. Stack: {ex.StackTrace}";
                    _logger.LogError("Full exception details: {FullDetails}", fullErrorDetails);
                    
                    return StatusCode(500, new { 
                        message = "Database operation failed", 
                        details = ex.Message, 
                        innerException = innerMessage,
                        fullError = fullErrorDetails
                    });
                }

                return Ok(new { 
                    message = "File uploaded successfully and processed in chunks.", 
                    fileName = file.FileName, 
                    chunksProcessed = pineconeVectors.Count,
                    totalFileSize = fileContent.Length,
                    pineconeResult 
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Exception while generating embedding or upserting vector for file {File}", file.FileName);
                return StatusCode(500, new { message = "Server error during embedding/upsert.", details = ex.Message });
            }
        }
    }
}