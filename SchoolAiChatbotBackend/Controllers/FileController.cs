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
                string textForEmbedding = looksBinary ? file.FileName : System.Text.Encoding.UTF8.GetString(rawBytes);

                var embedding = await _chatService.GetEmbeddingAsync(string.IsNullOrWhiteSpace(textForEmbedding) ? file.FileName : textForEmbedding);

                bool hasNonZero = embedding != null && embedding.Any(v => System.Math.Abs(v) > 1e-9f);
                if (!hasNonZero)
                {
                    embedding = await _chatService.GetEmbeddingAsync(string.IsNullOrWhiteSpace(textForEmbedding) ? file.FileName : textForEmbedding);
                    hasNonZero = embedding != null && embedding.Any(v => System.Math.Abs(v) > 1e-9f);
                }

                if (!hasNonZero)
                {
                    return StatusCode(500, new { message = "Embedding generation failed (all zero). Please retry or check the content." });
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
                    Id = Guid.NewGuid().ToString(),
                    Values = vectorValues,
                    Metadata = new Dictionary<string, object>
                    {

                        { "Class", Class },
                        { "fileName", file.FileName },
                        { "subject", subject },
                        { "chapter", chapter }
                    }
                };

                var upsertRequest = new SchoolAiChatbotBackend.Models.PineconeUpsertRequest
                {
                    Vectors = new List<SchoolAiChatbotBackend.Models.PineconeVector> { pineconeVector }
                };

                var (ok, result) = await _pineconeService.UpsertVectorsAsync(upsertRequest);
                pineconeResult = result;

                if (!ok)
                {
                    return StatusCode(500, new { message = "File uploaded but vector upsert failed.", details = result });
                }

                try
                {
                    var uploadedFile = new UploadedFile
                    {
                        FileName = file.FileName,
                        FilePath = filePath,
                        UploadDate = DateTime.UtcNow,
                        EmbeddingDimension = vectorValues.Count,
                        EmbeddingVector = string.Join(",", vectorValues),
                       
                    };
                    _dbContext.UploadedFiles.Add(uploadedFile);
                    await _dbContext.SaveChangesAsync();

                    // Insert records into SyllabusChunks table
                    foreach (var chunk in vectorValues.Chunk(256)) // Assuming chunk size of 256
                    {
                        var syllabusChunk = new SyllabusChunk
                        {
                            Subject = subject,
                            Grade = Class,
                            Chapter = chapter,
                            ChunkText = string.Join(",", chunk),
                            UploadedFileId = uploadedFile.Id,
                            PineconeVectorId = pineconeVector.Id
                        };
                        _dbContext.SyllabusChunks.Add(syllabusChunk);
                    }
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save UploadedFile or SyllabusChunks to DB for {File}", file.FileName);
                }

                return Ok(new { message = "File uploaded successfully and vector upserted.", fileName = file.FileName, pineconeResult });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Exception while generating embedding or upserting vector for file {File}", file.FileName);
                return StatusCode(500, new { message = "Server error during embedding/upsert.", details = ex.Message });
            }
        }
    }
}