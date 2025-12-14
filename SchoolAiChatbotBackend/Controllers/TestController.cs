using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Services;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IBlobStorageService _blobStorageService;

        public TestController(AppDbContext dbContext, IBlobStorageService blobStorageService)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "Backend is working!",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        [HttpGet("config")]
        public IActionResult Config()
        {
            return Ok(new
            {
                hasOpenAIKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OpenAI__ApiKey")),
                hasJWTKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT__SecretKey")),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }

        [HttpGet("syllabus")]
        public async Task<IActionResult> GetSyllabus()
        {
            try
            {
                var syllabusCount = await _dbContext.SyllabusChunks.CountAsync();
                var syllabusChunks = await _dbContext.SyllabusChunks
                    .Take(10)
                    .Select(s => new
                    {
                        s.Id,
                        s.Subject,
                        s.Grade,
                        s.Chapter,
                        ChunkPreview = s.ChunkText.Length > 100 ? s.ChunkText.Substring(0, 100) + "..." : s.ChunkText
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Syllabus data fetched from Azure SQL successfully!",
                    totalCount = syllabusCount,
                    sampleData = syllabusChunks,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to fetch syllabus from database",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("syllabus/subjects")]
        public async Task<IActionResult> GetSyllabusSubjects()
        {
            try
            {
                var subjects = await _dbContext.SyllabusChunks
                    .GroupBy(s => new { s.Subject, s.Grade })
                    .Select(g => new
                    {
                        Subject = g.Key.Subject,
                        Grade = g.Key.Grade,
                        ChunkCount = g.Count()
                    })
                    .OrderBy(x => x.Grade)
                    .ThenBy(x => x.Subject)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Syllabus subjects fetched successfully!",
                    subjects = subjects,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to fetch syllabus subjects",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("db-connection")]
        public async Task<IActionResult> TestDbConnection()
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                return Ok(new
                {
                    success = canConnect,
                    message = canConnect ? "Database connection successful!" : "Cannot connect to database",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database connection failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("blobs")]
        public async Task<IActionResult> ListBlobs([FromQuery] string? prefix = null)
        {
            try
            {
                if (!_blobStorageService.IsConfigured)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Blob storage is not configured",
                        timestamp = DateTime.UtcNow
                    });
                }

                var blobs = await _blobStorageService.ListBlobsAsync(prefix);
                
                // Group blobs by folder structure for easier viewing
                var byFolder = blobs
                    .GroupBy(b => string.Join("/", b.Name.Split('/').Take(b.Name.Split('/').Length - 1)))
                    .Select(g => new
                    {
                        Folder = g.Key,
                        FileCount = g.Count(),
                        TotalSize = g.Sum(b => b.Size),
                        Files = g.Select(b => new 
                        { 
                            FileName = b.Name.Split('/').Last(), 
                            b.Size, 
                            b.LastModified 
                        }).ToList()
                    })
                    .OrderBy(x => x.Folder)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Found {blobs.Count} blobs in storage",
                    totalBlobs = blobs.Count,
                    byFolder = byFolder,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to list blobs",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("blobs/textbooks")]
        public async Task<IActionResult> ListTextbooks()
        {
            try
            {
                if (!_blobStorageService.IsConfigured)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Blob storage is not configured",
                        timestamp = DateTime.UtcNow
                    });
                }

                // List blobs with "12/" prefix (Grade 12 textbooks)
                var blobs = await _blobStorageService.ListBlobsAsync("12/");
                
                // Parse textbook info from path: 12/{Subject}/{filename}
                var textbooks = blobs
                    .Where(b => b.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    .Select(b => {
                        var parts = b.Name.Split('/');
                        return new
                        {
                            Grade = parts.Length > 0 ? parts[0] : "Unknown",
                            Subject = parts.Length > 1 ? parts[1] : "Unknown",
                            FileName = parts.Length > 2 ? parts[2] : b.Name,
                            FullPath = b.Name,
                            b.Size,
                            b.LastModified,
                            b.Url
                        };
                    })
                    .ToList();

                var bySubject = textbooks
                    .GroupBy(t => t.Subject)
                    .Select(g => new
                    {
                        Subject = g.Key,
                        FileCount = g.Count(),
                        Files = g.Select(f => new { f.FileName, f.Size }).ToList()
                    })
                    .OrderBy(x => x.Subject)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Found {textbooks.Count} Grade 12 textbook PDFs",
                    totalTextbooks = textbooks.Count,
                    subjects = bySubject,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to list textbooks",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// List all model question papers in the model-questions container
        /// GET /api/test/blobs/model-questions
        /// </summary>
        [HttpGet("blobs/model-questions")]
        public async Task<IActionResult> ListModelQuestionPapers()
        {
            try
            {
                if (!_blobStorageService.IsConfigured)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Blob storage is not configured",
                        timestamp = DateTime.UtcNow
                    });
                }

                // List all blobs in model-questions container
                var blobs = await _blobStorageService.ListBlobsAsync(null, "model-questions");
                
                // Parse model question paper info from path: {State}/{Grade}/{Subject}/{filename}
                var papers = blobs
                    .Where(b => b.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    .Select(b => {
                        var parts = b.Name.Split('/');
                        return new
                        {
                            State = parts.Length > 0 ? parts[0] : "Unknown",
                            Grade = parts.Length > 1 ? parts[1] : "Unknown",
                            Subject = parts.Length > 2 ? parts[2] : "Unknown",
                            FileName = parts.Length > 3 ? parts[3] : b.Name,
                            FullPath = b.Name,
                            b.Size,
                            b.LastModified,
                            b.Url
                        };
                    })
                    .ToList();

                var byState = papers
                    .GroupBy(p => p.State)
                    .Select(g => new
                    {
                        State = g.Key,
                        Grades = g.GroupBy(p => p.Grade)
                            .Select(gg => new
                            {
                                Grade = gg.Key,
                                Subjects = gg.GroupBy(p => p.Subject)
                                    .Select(gs => new
                                    {
                                        Subject = gs.Key,
                                        FileCount = gs.Count(),
                                        Files = gs.Select(f => new { f.FileName, f.Size }).ToList()
                                    }).ToList()
                            }).ToList()
                    })
                    .OrderBy(x => x.State)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Found {papers.Count} model question papers",
                    totalPapers = papers.Count,
                    container = "model-questions",
                    papersByState = byState,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to list model question papers",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// List all evaluation sheets in the evaluation-sheets container
        /// GET /api/test/blobs/evaluation-sheets
        /// </summary>
        [HttpGet("blobs/evaluation-sheets")]
        public async Task<IActionResult> ListEvaluationSheets()
        {
            try
            {
                if (!_blobStorageService.IsConfigured)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Blob storage is not configured",
                        timestamp = DateTime.UtcNow
                    });
                }

                // List all blobs in evaluation-sheets container
                var blobs = await _blobStorageService.ListBlobsAsync(null, "evaluation-sheets");
                
                // Parse evaluation sheet info from path: {State}/{Grade}/{Subject}/{filename}
                var sheets = blobs
                    .Where(b => b.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    .Select(b => {
                        var parts = b.Name.Split('/');
                        return new
                        {
                            State = parts.Length > 0 ? parts[0] : "Unknown",
                            Grade = parts.Length > 1 ? parts[1] : "Unknown",
                            Subject = parts.Length > 2 ? parts[2] : "Unknown",
                            FileName = parts.Length > 3 ? parts[3] : b.Name,
                            FullPath = b.Name,
                            b.Size,
                            b.LastModified,
                            b.Url
                        };
                    })
                    .ToList();

                var byState = sheets
                    .GroupBy(p => p.State)
                    .Select(g => new
                    {
                        State = g.Key,
                        Grades = g.GroupBy(p => p.Grade)
                            .Select(gg => new
                            {
                                Grade = gg.Key,
                                Subjects = gg.GroupBy(p => p.Subject)
                                    .Select(gs => new
                                    {
                                        Subject = gs.Key,
                                        FileCount = gs.Count(),
                                        Files = gs.Select(f => new { f.FileName, f.Size }).ToList()
                                    }).ToList()
                            }).ToList()
                    })
                    .OrderBy(x => x.State)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Found {sheets.Count} evaluation sheets",
                    totalSheets = sheets.Count,
                    container = "evaluation-sheets",
                    sheetsByState = byState,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to list evaluation sheets",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}