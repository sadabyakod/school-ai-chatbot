using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;

namespace SchoolAiChatbotBackend.Controllers
{
    /// <summary>
    /// Controller for managing Model Question Papers
    /// Upload, download, and list question papers by class and subject
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionPapersController : ControllerBase
    {
        private readonly ILogger<QuestionPapersController> _logger;
        private readonly IBlobStorageService _blobStorageService;
        private readonly AppDbContext _dbContext;

        public QuestionPapersController(
            ILogger<QuestionPapersController> logger,
            IBlobStorageService blobStorageService,
            AppDbContext dbContext)
        {
            _logger = logger;
            _blobStorageService = blobStorageService;
            _dbContext = dbContext;
        }

        // ==========================================
        // UPLOAD ENDPOINTS
        // ==========================================

        /// <summary>
        /// Upload a model question paper
        /// POST /api/questionpapers/upload
        /// </summary>
        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100 MB
        public async Task<IActionResult> UploadQuestionPaper()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var file = form.Files.GetFile("file");
                var subject = form["subject"].ToString();
                var grade = form["grade"].ToString();
                var medium = form["medium"].ToString();
                var academicYear = form["academicYear"].ToString();
                var chapter = form["chapter"].ToString();
                var paperType = form["paperType"].ToString();

                _logger.LogInformation("Question paper upload: File={FileName}, Subject={Subject}, Grade={Grade}",
                    file?.FileName ?? "null", subject, grade);

                // Validation
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { status = "error", message = "No file uploaded." });
                }

                if (string.IsNullOrWhiteSpace(subject))
                {
                    return BadRequest(new { status = "error", message = "Subject is required." });
                }

                if (string.IsNullOrWhiteSpace(grade))
                {
                    return BadRequest(new { status = "error", message = "Grade/Class is required." });
                }

                // Default values
                if (string.IsNullOrWhiteSpace(medium)) medium = "English";
                if (string.IsNullOrWhiteSpace(paperType)) paperType = "Model";

                // Upload to Azure Blob Storage
                string blobUrl;
                using (var stream = file.OpenReadStream())
                {
                    // Use question-papers container with folder structure
                    var blobName = $"question-papers/{grade}/{subject}/{Guid.NewGuid()}_{file.FileName}";
                    blobUrl = await _blobStorageService.UploadFileToBlobAsync(
                        file.FileName,
                        stream,
                        file.ContentType,
                        grade,
                        subject,
                        null);
                }

                // Save metadata to database
                var questionPaper = new ModelQuestionPaper
                {
                    FileName = file.FileName,
                    BlobUrl = blobUrl,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    Subject = subject,
                    Grade = grade,
                    Medium = medium,
                    AcademicYear = string.IsNullOrWhiteSpace(academicYear) ? null : academicYear,
                    Chapter = string.IsNullOrWhiteSpace(chapter) ? null : chapter,
                    PaperType = paperType,
                    FileSize = file.Length,
                    ContentType = file.ContentType
                };

                _dbContext.ModelQuestionPapers.Add(questionPaper);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Question paper uploaded successfully: Id={Id}, FileName={FileName}",
                    questionPaper.Id, questionPaper.FileName);

                return Ok(new
                {
                    status = "success",
                    message = "Question paper uploaded successfully.",
                    data = new
                    {
                        id = questionPaper.Id,
                        fileName = questionPaper.FileName,
                        subject = questionPaper.Subject,
                        grade = questionPaper.Grade,
                        medium = questionPaper.Medium,
                        academicYear = questionPaper.AcademicYear,
                        paperType = questionPaper.PaperType,
                        blobUrl = questionPaper.BlobUrl,
                        uploadedAt = questionPaper.UploadedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading question paper");
                return StatusCode(500, new { status = "error", message = "Failed to upload question paper." });
            }
        }

        // ==========================================
        // LIST ENDPOINTS
        // ==========================================

        /// <summary>
        /// Get all question papers grouped by subject and grade
        /// GET /api/questionpapers
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllQuestionPapers(
            [FromQuery] string? grade = null,
            [FromQuery] string? subject = null,
            [FromQuery] string? medium = null,
            [FromQuery] string? academicYear = null)
        {
            try
            {
                var query = _dbContext.ModelQuestionPapers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(grade))
                    query = query.Where(p => p.Grade == grade);

                if (!string.IsNullOrWhiteSpace(subject))
                    query = query.Where(p => p.Subject.ToLower() == subject.ToLower());

                if (!string.IsNullOrWhiteSpace(medium))
                    query = query.Where(p => p.Medium == medium);

                if (!string.IsNullOrWhiteSpace(academicYear))
                    query = query.Where(p => p.AcademicYear == academicYear);

                var papers = await query
                    .GroupBy(p => new { p.Subject, p.Grade, p.Medium })
                    .Select(g => new
                    {
                        Subject = g.Key.Subject,
                        Grade = g.Key.Grade,
                        Medium = g.Key.Medium,
                        PaperCount = g.Count(),
                        LatestUpload = g.Max(p => p.UploadedAt),
                        Papers = g.Select(p => new
                        {
                            p.Id,
                            p.FileName,
                            p.BlobUrl,
                            p.AcademicYear,
                            p.Chapter,
                            p.PaperType,
                            p.FileSize,
                            p.UploadedAt
                        }).OrderByDescending(p => p.UploadedAt).ToList()
                    })
                    .OrderBy(g => g.Subject)
                    .ThenBy(g => g.Grade)
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = papers.Count,
                    totalPapers = papers.Sum(p => p.PaperCount),
                    questionPapers = papers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching question papers");
                return StatusCode(500, new { status = "error", message = "Failed to fetch question papers." });
            }
        }

        /// <summary>
        /// Get question papers for a specific subject
        /// GET /api/questionpapers/subject/{subject}
        /// </summary>
        [HttpGet("subject/{subject}")]
        public async Task<IActionResult> GetBySubject(
            string subject,
            [FromQuery] string? grade = null,
            [FromQuery] string? academicYear = null)
        {
            try
            {
                var query = _dbContext.ModelQuestionPapers
                    .Where(p => p.Subject.ToLower() == subject.ToLower());

                if (!string.IsNullOrWhiteSpace(grade))
                    query = query.Where(p => p.Grade == grade);

                if (!string.IsNullOrWhiteSpace(academicYear))
                    query = query.Where(p => p.AcademicYear == academicYear);

                var papers = await query
                    .OrderByDescending(p => p.UploadedAt)
                    .Select(p => new
                    {
                        p.Id,
                        p.FileName,
                        p.Subject,
                        p.Grade,
                        p.Medium,
                        p.AcademicYear,
                        p.Chapter,
                        p.PaperType,
                        p.BlobUrl,
                        p.FileSize,
                        p.UploadedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    subject,
                    count = papers.Count,
                    papers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching question papers for subject: {Subject}", subject);
                return StatusCode(500, new { status = "error", message = "Failed to fetch question papers." });
            }
        }

        /// <summary>
        /// Get question papers for a specific grade/class
        /// GET /api/questionpapers/grade/{grade}
        /// </summary>
        [HttpGet("grade/{grade}")]
        public async Task<IActionResult> GetByGrade(
            string grade,
            [FromQuery] string? subject = null,
            [FromQuery] string? academicYear = null)
        {
            try
            {
                var query = _dbContext.ModelQuestionPapers
                    .Where(p => p.Grade.ToLower() == grade.ToLower());

                if (!string.IsNullOrWhiteSpace(subject))
                    query = query.Where(p => p.Subject.ToLower() == subject.ToLower());

                if (!string.IsNullOrWhiteSpace(academicYear))
                    query = query.Where(p => p.AcademicYear == academicYear);

                var papersBySubject = await query
                    .GroupBy(p => p.Subject)
                    .Select(g => new
                    {
                        Subject = g.Key,
                        PaperCount = g.Count(),
                        Papers = g.Select(p => new
                        {
                            p.Id,
                            p.FileName,
                            p.AcademicYear,
                            p.Chapter,
                            p.PaperType,
                            p.BlobUrl,
                            p.FileSize,
                            p.UploadedAt
                        }).OrderByDescending(p => p.UploadedAt).ToList()
                    })
                    .OrderBy(g => g.Subject)
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    grade,
                    subjectCount = papersBySubject.Count,
                    totalPapers = papersBySubject.Sum(s => s.PaperCount),
                    subjects = papersBySubject
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching question papers for grade: {Grade}", grade);
                return StatusCode(500, new { status = "error", message = "Failed to fetch question papers." });
            }
        }

        /// <summary>
        /// Get available grades that have question papers
        /// GET /api/questionpapers/grades
        /// </summary>
        [HttpGet("grades")]
        public async Task<IActionResult> GetAvailableGrades([FromQuery] string? medium = null)
        {
            try
            {
                var query = _dbContext.ModelQuestionPapers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(medium))
                    query = query.Where(p => p.Medium == medium);

                var grades = await query
                    .GroupBy(p => p.Grade)
                    .Select(g => new
                    {
                        Grade = g.Key,
                        SubjectCount = g.Select(p => p.Subject).Distinct().Count(),
                        PaperCount = g.Count()
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
        /// Get available subjects for a grade
        /// GET /api/questionpapers/subjects
        /// </summary>
        [HttpGet("subjects")]
        public async Task<IActionResult> GetAvailableSubjects(
            [FromQuery] string? grade = null,
            [FromQuery] string? medium = null)
        {
            try
            {
                var query = _dbContext.ModelQuestionPapers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(grade))
                    query = query.Where(p => p.Grade == grade);

                if (!string.IsNullOrWhiteSpace(medium))
                    query = query.Where(p => p.Medium == medium);

                var subjects = await query
                    .GroupBy(p => p.Subject)
                    .Select(g => new
                    {
                        Subject = g.Key,
                        PaperCount = g.Count(),
                        Grades = g.Select(p => p.Grade).Distinct().ToList()
                    })
                    .OrderBy(s => s.Subject)
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    grade,
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
        /// Get available academic years
        /// GET /api/questionpapers/years
        /// </summary>
        [HttpGet("years")]
        public async Task<IActionResult> GetAvailableYears(
            [FromQuery] string? grade = null,
            [FromQuery] string? subject = null)
        {
            try
            {
                var query = _dbContext.ModelQuestionPapers
                    .Where(p => p.AcademicYear != null);

                if (!string.IsNullOrWhiteSpace(grade))
                    query = query.Where(p => p.Grade == grade);

                if (!string.IsNullOrWhiteSpace(subject))
                    query = query.Where(p => p.Subject.ToLower() == subject.ToLower());

                var years = await query
                    .GroupBy(p => p.AcademicYear)
                    .Select(g => new
                    {
                        AcademicYear = g.Key,
                        PaperCount = g.Count()
                    })
                    .OrderByDescending(y => y.AcademicYear)
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = years.Count,
                    years
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available years");
                return StatusCode(500, new { status = "error", message = "Failed to fetch years." });
            }
        }

        // ==========================================
        // DOWNLOAD ENDPOINTS
        // ==========================================

        /// <summary>
        /// Get download URL for a specific question paper
        /// GET /api/questionpapers/download/{id}
        /// </summary>
        [HttpGet("download/{id}")]
        public async Task<IActionResult> GetDownloadUrl(int id)
        {
            try
            {
                var paper = await _dbContext.ModelQuestionPapers
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (paper == null)
                {
                    return NotFound(new { status = "error", message = "Question paper not found." });
                }

                return Ok(new
                {
                    status = "success",
                    data = new
                    {
                        id = paper.Id,
                        fileName = paper.FileName,
                        subject = paper.Subject,
                        grade = paper.Grade,
                        medium = paper.Medium,
                        academicYear = paper.AcademicYear,
                        chapter = paper.Chapter,
                        paperType = paper.PaperType,
                        downloadUrl = paper.BlobUrl,
                        fileSize = paper.FileSize,
                        contentType = paper.ContentType,
                        uploadedAt = paper.UploadedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting download URL for paper: {Id}", id);
                return StatusCode(500, new { status = "error", message = "Failed to get download URL." });
            }
        }

        /// <summary>
        /// Delete a question paper
        /// DELETE /api/questionpapers/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestionPaper(int id)
        {
            try
            {
                var paper = await _dbContext.ModelQuestionPapers
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (paper == null)
                {
                    return NotFound(new { status = "error", message = "Question paper not found." });
                }

                // Delete from blob storage (optional - keep for now)
                // await _blobStorageService.DeleteBlobAsync(paper.BlobUrl);

                _dbContext.ModelQuestionPapers.Remove(paper);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Question paper deleted: Id={Id}, FileName={FileName}", id, paper.FileName);

                return Ok(new
                {
                    status = "success",
                    message = "Question paper deleted successfully.",
                    deletedId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question paper: {Id}", id);
                return StatusCode(500, new { status = "error", message = "Failed to delete question paper." });
            }
        }
    }
}
