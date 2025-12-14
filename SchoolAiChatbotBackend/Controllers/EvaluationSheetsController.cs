using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;

namespace SchoolAiChatbotBackend.Controllers
{
    /// <summary>
    /// Controller for managing Evaluation Sheets (Answer Scheme/Marking Scheme)
    /// Upload, download, and list evaluation sheets by class and subject
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluationSheetsController : ControllerBase
    {
        private readonly ILogger<EvaluationSheetsController> _logger;
        private readonly IBlobStorageService _blobStorageService;
        private readonly AppDbContext _dbContext;

        public EvaluationSheetsController(
            ILogger<EvaluationSheetsController> logger,
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
        /// Upload an evaluation sheet (answer scheme/marking scheme)
        /// POST /api/evaluationsheets/upload
        /// </summary>
        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100 MB
        public async Task<IActionResult> UploadEvaluationSheet()
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
                var sheetType = form["sheetType"].ToString();
                var state = form["state"].ToString();

                _logger.LogInformation("Evaluation sheet upload: File={FileName}, Subject={Subject}, Grade={Grade}, State={State}",
                    file?.FileName ?? "null", subject, grade, state);

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
                if (string.IsNullOrWhiteSpace(sheetType)) sheetType = "Model";
                if (string.IsNullOrWhiteSpace(state)) state = "Karnataka";

                // Upload to Azure Blob Storage with folder structure:
                // Container: evaluation-sheets
                // Path: {State}/{Class}/{Subject}/{unique_filename}
                // Example: evaluation-sheets (container) / Karnataka/12/Physics/2023_Evaluation_Scheme.pdf
                string blobUrl;
                using (var stream = file.OpenReadStream())
                {
                    var folderPath = $"{state}/{grade}/{subject}";
                    blobUrl = await _blobStorageService.UploadWithCustomPathAsync(
                        file.FileName,
                        stream,
                        folderPath,
                        file.ContentType,
                        "evaluation-sheets");  // Separate container for evaluation sheets
                }

                // Save metadata to database
                var evaluationSheet = new EvaluationSheet
                {
                    FileName = file.FileName,
                    BlobUrl = blobUrl,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    Subject = subject,
                    Grade = grade,
                    State = state,
                    Medium = medium,
                    AcademicYear = string.IsNullOrWhiteSpace(academicYear) ? null : academicYear,
                    Chapter = string.IsNullOrWhiteSpace(chapter) ? null : chapter,
                    SheetType = sheetType,
                    FileSize = file.Length,
                    ContentType = file.ContentType
                };

                _dbContext.EvaluationSheets.Add(evaluationSheet);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Evaluation sheet uploaded successfully: Id={Id}, FileName={FileName}",
                    evaluationSheet.Id, evaluationSheet.FileName);

                return Ok(new
                {
                    status = "success",
                    message = "Evaluation sheet uploaded successfully.",
                    data = new
                    {
                        id = evaluationSheet.Id,
                        fileName = evaluationSheet.FileName,
                        subject = evaluationSheet.Subject,
                        grade = evaluationSheet.Grade,
                        state = evaluationSheet.State,
                        medium = evaluationSheet.Medium,
                        academicYear = evaluationSheet.AcademicYear,
                        sheetType = evaluationSheet.SheetType,
                        blobUrl = evaluationSheet.BlobUrl,
                        uploadedAt = evaluationSheet.UploadedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading evaluation sheet");
                return StatusCode(500, new { status = "error", message = "Failed to upload evaluation sheet." });
            }
        }

        // ==========================================
        // LIST ENDPOINTS
        // ==========================================

        /// <summary>
        /// Get all evaluation sheets with optional filtering
        /// GET /api/evaluationsheets?subject=Physics&grade=12&state=Karnataka
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEvaluationSheets(
            [FromQuery] string? subject = null,
            [FromQuery] string? grade = null,
            [FromQuery] string? state = null,
            [FromQuery] string? year = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _dbContext.EvaluationSheets.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(subject))
                    query = query.Where(e => e.Subject.ToLower() == subject.ToLower());

                if (!string.IsNullOrWhiteSpace(grade))
                    query = query.Where(e => e.Grade == grade);

                if (!string.IsNullOrWhiteSpace(state))
                    query = query.Where(e => e.State.ToLower() == state.ToLower());

                if (!string.IsNullOrWhiteSpace(year))
                    query = query.Where(e => e.AcademicYear == year);

                var totalCount = await query.CountAsync();

                // Group by subject and grade
                var groupedSheets = await query
                    .OrderByDescending(e => e.UploadedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .GroupBy(e => new { e.Subject, e.Grade, e.Medium, e.State })
                    .Select(g => new
                    {
                        subject = g.Key.Subject,
                        grade = g.Key.Grade,
                        medium = g.Key.Medium,
                        state = g.Key.State,
                        sheetCount = g.Count(),
                        latestUpload = g.Max(e => e.UploadedAt),
                        sheets = g.Select(e => new
                        {
                            id = e.Id,
                            fileName = e.FileName,
                            blobUrl = e.BlobUrl,
                            academicYear = e.AcademicYear,
                            sheetType = e.SheetType,
                            fileSize = e.FileSize,
                            uploadedAt = e.UploadedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = groupedSheets.Count,
                    totalSheets = totalCount,
                    page,
                    pageSize,
                    evaluationSheets = groupedSheets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching evaluation sheets");
                return StatusCode(500, new { status = "error", message = "Failed to fetch evaluation sheets." });
            }
        }

        /// <summary>
        /// Get evaluation sheets by subject
        /// GET /api/evaluationsheets/subject/Physics
        /// </summary>
        [HttpGet("subject/{subject}")]
        public async Task<IActionResult> GetBySubject(string subject)
        {
            try
            {
                var sheets = await _dbContext.EvaluationSheets
                    .Where(e => e.Subject.ToLower() == subject.ToLower())
                    .OrderByDescending(e => e.UploadedAt)
                    .Select(e => new
                    {
                        id = e.Id,
                        fileName = e.FileName,
                        subject = e.Subject,
                        grade = e.Grade,
                        state = e.State,
                        medium = e.Medium,
                        academicYear = e.AcademicYear,
                        sheetType = e.SheetType,
                        blobUrl = e.BlobUrl,
                        uploadedAt = e.UploadedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = sheets.Count,
                    subject,
                    evaluationSheets = sheets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching evaluation sheets by subject");
                return StatusCode(500, new { status = "error", message = "Failed to fetch evaluation sheets." });
            }
        }

        /// <summary>
        /// Get evaluation sheets by grade
        /// GET /api/evaluationsheets/grade/12
        /// </summary>
        [HttpGet("grade/{grade}")]
        public async Task<IActionResult> GetByGrade(string grade)
        {
            try
            {
                var sheets = await _dbContext.EvaluationSheets
                    .Where(e => e.Grade == grade)
                    .OrderByDescending(e => e.UploadedAt)
                    .Select(e => new
                    {
                        id = e.Id,
                        fileName = e.FileName,
                        subject = e.Subject,
                        grade = e.Grade,
                        state = e.State,
                        medium = e.Medium,
                        academicYear = e.AcademicYear,
                        sheetType = e.SheetType,
                        blobUrl = e.BlobUrl,
                        uploadedAt = e.UploadedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    status = "success",
                    count = sheets.Count,
                    grade,
                    evaluationSheets = sheets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching evaluation sheets by grade");
                return StatusCode(500, new { status = "error", message = "Failed to fetch evaluation sheets." });
            }
        }

        /// <summary>
        /// Get available subjects with evaluation sheets
        /// GET /api/evaluationsheets/subjects
        /// </summary>
        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            try
            {
                var subjects = await _dbContext.EvaluationSheets
                    .Select(e => e.Subject)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                return Ok(new { subjects });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subjects");
                return StatusCode(500, new { status = "error", message = "Failed to fetch subjects." });
            }
        }

        /// <summary>
        /// Get available grades with evaluation sheets
        /// GET /api/evaluationsheets/grades
        /// </summary>
        [HttpGet("grades")]
        public async Task<IActionResult> GetGrades()
        {
            try
            {
                var grades = await _dbContext.EvaluationSheets
                    .Select(e => e.Grade)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync();

                return Ok(new { grades });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching grades");
                return StatusCode(500, new { status = "error", message = "Failed to fetch grades." });
            }
        }

        /// <summary>
        /// Get available academic years
        /// GET /api/evaluationsheets/years
        /// </summary>
        [HttpGet("years")]
        public async Task<IActionResult> GetYears()
        {
            try
            {
                var years = await _dbContext.EvaluationSheets
                    .Where(e => e.AcademicYear != null)
                    .Select(e => e.AcademicYear!)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync();

                return Ok(new { years });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching years");
                return StatusCode(500, new { status = "error", message = "Failed to fetch years." });
            }
        }

        // ==========================================
        // DOWNLOAD ENDPOINT
        // ==========================================

        /// <summary>
        /// Download an evaluation sheet by ID
        /// GET /api/evaluationsheets/download/1
        /// </summary>
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadEvaluationSheet(int id)
        {
            try
            {
                var sheet = await _dbContext.EvaluationSheets.FindAsync(id);

                if (sheet == null)
                {
                    return NotFound(new { status = "error", message = "Evaluation sheet not found." });
                }

                // Extract blob name from URL
                var uri = new Uri(sheet.BlobUrl);
                var blobName = uri.AbsolutePath.TrimStart('/').Replace("evaluation-sheets/", "");

                var stream = await _blobStorageService.DownloadAsync(blobName, "evaluation-sheets");

                if (stream == null)
                {
                    return NotFound(new { status = "error", message = "File not found in storage." });
                }

                return File(stream, sheet.ContentType ?? "application/pdf", sheet.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading evaluation sheet");
                return StatusCode(500, new { status = "error", message = "Failed to download evaluation sheet." });
            }
        }

        /// <summary>
        /// Delete an evaluation sheet by ID
        /// DELETE /api/evaluationsheets/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvaluationSheet(int id)
        {
            try
            {
                var sheet = await _dbContext.EvaluationSheets.FindAsync(id);

                if (sheet == null)
                {
                    return NotFound(new { status = "error", message = "Evaluation sheet not found." });
                }

                // Delete from blob storage
                try
                {
                    var uri = new Uri(sheet.BlobUrl);
                    var blobName = uri.AbsolutePath.TrimStart('/').Replace("evaluation-sheets/", "");
                    await _blobStorageService.DeleteAsync(blobName, "evaluation-sheets");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete blob for evaluation sheet {Id}", id);
                }

                // Delete from database
                _dbContext.EvaluationSheets.Remove(sheet);
                await _dbContext.SaveChangesAsync();

                return Ok(new { status = "success", message = "Evaluation sheet deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting evaluation sheet");
                return StatusCode(500, new { status = "error", message = "Failed to delete evaluation sheet." });
            }
        }
    }
}
