using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.DTOs;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Controllers
{
    /// <summary>
    /// Controller for exam analytics and reporting
    /// Provides endpoints for teachers to review submissions and analyze exam performance
    /// </summary>
    [ApiController]
    [Route("api/exam")]
    public class ExamAnalyticsController : ControllerBase
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamStorageService _examStorageService;
        private readonly ILogger<ExamAnalyticsController> _logger;

        public ExamAnalyticsController(
            IExamRepository examRepository,
            IExamStorageService examStorageService,
            ILogger<ExamAnalyticsController> logger)
        {
            _examRepository = examRepository;
            _examStorageService = examStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Get all submissions for a specific exam
        /// </summary>
        /// <param name="examId">The exam ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <returns>Paginated list of submissions</returns>
        [HttpGet("{examId}/submissions")]
        [ProducesResponseType(typeof(PaginatedListDto<ExamSubmissionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PaginatedListDto<ExamSubmissionDto>>> GetExamSubmissions(
            string examId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Getting submissions for exam {ExamId}, page {Page}", examId, page);

                // Verify exam exists
                var exam = await _examStorageService.GetExamAsync(examId);
                if (exam == null)
                {
                    return NotFound(new { error = $"Exam {examId} not found" });
                }

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                // Get all submissions for this exam
                var allSubmissions = await _examRepository.GetAllSubmissionsByExamAsync(examId);

                // Convert to DTOs
                var submissionDtos = new List<ExamSubmissionDto>();
                foreach (var (mcqSubmission, writtenSubmission) in allSubmissions)
                {
                    var dto = await BuildExamSubmissionDto(examId, mcqSubmission, writtenSubmission);
                    submissionDtos.Add(dto);
                }

                // Order by latest submission time
                submissionDtos = submissionDtos.OrderByDescending(s => s.LatestSubmissionTime).ToList();

                // Apply pagination
                var totalCount = submissionDtos.Count;
                var paginatedItems = submissionDtos
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedListDto<ExamSubmissionDto>
                {
                    Items = paginatedItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                _logger.LogInformation("Retrieved {Count} submissions for exam {ExamId}", totalCount, examId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submissions for exam {ExamId}", examId);
                return StatusCode(500, new { error = "An error occurred while retrieving submissions" });
            }
        }

        /// <summary>
        /// Get detailed submission information for a specific student's attempt
        /// </summary>
        /// <param name="examId">The exam ID</param>
        /// <param name="studentId">The student ID</param>
        /// <returns>Detailed submission information</returns>
        [HttpGet("{examId}/submissions/{studentId}")]
        [ProducesResponseType(typeof(ExamSubmissionDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ExamSubmissionDetailDto>> GetStudentSubmissionDetail(
            string examId,
            string studentId)
        {
            try
            {
                _logger.LogInformation("Getting submission detail for exam {ExamId}, student {StudentId}", examId, studentId);

                // Verify exam exists
                var exam = await _examStorageService.GetExamAsync(examId);
                if (exam == null)
                {
                    return NotFound(new { error = $"Exam {examId} not found" });
                }

                // Get submissions
                var mcqSubmission = await _examRepository.GetMcqSubmissionAsync(examId, studentId);
                var writtenSubmission = await _examRepository.GetWrittenSubmissionByExamAndStudentAsync(examId, studentId);

                if (mcqSubmission == null && writtenSubmission == null)
                {
                    return NotFound(new { error = $"No submission found for student {studentId} in exam {examId}" });
                }

                // Build detailed DTO
                var detail = await BuildExamSubmissionDetailDto(exam, mcqSubmission, writtenSubmission);

                _logger.LogInformation("Retrieved submission detail for exam {ExamId}, student {StudentId}", examId, studentId);
                return Ok(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission detail for exam {ExamId}, student {StudentId}", examId, studentId);
                return StatusCode(500, new { error = "An error occurred while retrieving submission details" });
            }
        }

        /// <summary>
        /// Get all exams attempted by a specific student
        /// </summary>
        /// <param name="studentId">The student ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <returns>Paginated list of student's exam attempts</returns>
        [HttpGet("submissions/by-student/{studentId}")]
        [ProducesResponseType(typeof(PaginatedListDto<StudentExamHistoryDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PaginatedListDto<StudentExamHistoryDto>>> GetStudentExamHistory(
            string studentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Getting exam history for student {StudentId}, page {Page}", studentId, page);

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                // Get all submissions for this student
                var mcqSubmissions = await _examRepository.GetAllMcqSubmissionsByStudentAsync(studentId);
                var writtenSubmissions = await _examRepository.GetAllWrittenSubmissionsByStudentAsync(studentId);

                // Get unique exam IDs
                var examIds = mcqSubmissions.Select(s => s.ExamId)
                    .Union(writtenSubmissions.Select(s => s.ExamId))
                    .Distinct()
                    .ToList();

                if (!examIds.Any())
                {
                    return Ok(new PaginatedListDto<StudentExamHistoryDto>
                    {
                        Items = new List<StudentExamHistoryDto>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = pageSize
                    });
                }

                // Build history DTOs
                var historyList = new List<StudentExamHistoryDto>();
                foreach (var examId in examIds)
                {
                    var exam = await _examStorageService.GetExamAsync(examId);
                    if (exam == null) continue;

                    var mcqSub = mcqSubmissions.FirstOrDefault(s => s.ExamId == examId);
                    var writtenSub = writtenSubmissions.FirstOrDefault(s => s.ExamId == examId);

                    var history = await BuildStudentExamHistoryDto(exam, mcqSub, writtenSub);
                    historyList.Add(history);
                }

                // Order by attempt date (most recent first)
                historyList = historyList.OrderByDescending(h => h.AttemptedAt).ToList();

                // Apply pagination
                var totalCount = historyList.Count;
                var paginatedItems = historyList
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedListDto<StudentExamHistoryDto>
                {
                    Items = paginatedItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                _logger.LogInformation("Retrieved {Count} exam attempts for student {StudentId}", totalCount, studentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam history for student {StudentId}", studentId);
                return StatusCode(500, new { error = "An error occurred while retrieving student exam history" });
            }
        }

        /// <summary>
        /// Get summary statistics for an exam
        /// </summary>
        /// <param name="examId">The exam ID</param>
        /// <returns>Summary statistics including scores, completion rates, and status breakdown</returns>
        [HttpGet("{examId}/summary")]
        [ProducesResponseType(typeof(ExamSummaryDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ExamSummaryDto>> GetExamSummary(string examId)
        {
            try
            {
                _logger.LogInformation("Getting summary for exam {ExamId}", examId);

                // Verify exam exists
                var exam = await _examStorageService.GetExamAsync(examId);
                if (exam == null)
                {
                    return NotFound(new { error = $"Exam {examId} not found" });
                }

                // Get all submissions
                var allSubmissions = await _examRepository.GetAllSubmissionsByExamAsync(examId);

                if (!allSubmissions.Any())
                {
                    return Ok(new ExamSummaryDto
                    {
                        ExamId = examId,
                        ExamTitle = $"{exam.Subject} - {exam.Chapter}",
                        Subject = exam.Subject ?? "",
                        TotalSubmissions = 0,
                        CompletedSubmissions = 0,
                        PendingEvaluations = 0,
                        PartialSubmissions = 0
                    });
                }

                // Calculate statistics
                var submissionStats = new List<(double? score, double? totalMarks, SubmissionStatusType status, SubmissionType type)>();

                foreach (var (mcqSub, writtenSub) in allSubmissions)
                {
                    var (score, totalMarks) = CalculateTotalScore(mcqSub, writtenSub);
                    var status = DetermineSubmissionStatus(mcqSub, writtenSub);
                    var submissionType = DetermineSubmissionType(mcqSub, writtenSub);

                    submissionStats.Add((score, totalMarks, status, submissionType));
                }

                // Compute summary
                var completedScores = submissionStats
                    .Where(s => s.status == SubmissionStatusType.Completed && s.score.HasValue && s.totalMarks.HasValue)
                    .Select(s => (score: s.score!.Value, total: s.totalMarks!.Value))
                    .ToList();

                var summary = new ExamSummaryDto
                {
                    ExamId = examId,
                    ExamTitle = $"{exam.Subject} - {exam.Chapter}",
                    Subject = exam.Subject ?? "",
                    TotalSubmissions = submissionStats.Count,
                    CompletedSubmissions = submissionStats.Count(s => s.status == SubmissionStatusType.Completed),
                    PendingEvaluations = submissionStats.Count(s => s.status == SubmissionStatusType.PendingEvaluation),
                    PartialSubmissions = submissionStats.Count(s => s.status == SubmissionStatusType.PartiallyCompleted),
                    AverageScore = completedScores.Any() ? Math.Round(completedScores.Average(s => s.score), 2) : null,
                    MinScore = completedScores.Any() ? completedScores.Min(s => s.score) : null,
                    MaxScore = completedScores.Any() ? completedScores.Max(s => s.score) : null,
                    AveragePercentage = completedScores.Any() 
                        ? Math.Round(completedScores.Average(s => (s.score / s.total) * 100), 2) 
                        : null,
                    StatusBreakdown = submissionStats
                        .GroupBy(s => s.status)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    SubmissionTypeBreakdown = submissionStats
                        .GroupBy(s => s.type)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                _logger.LogInformation("Generated summary for exam {ExamId}: {Total} submissions, {Completed} completed", 
                    examId, summary.TotalSubmissions, summary.CompletedSubmissions);
                
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting summary for exam {ExamId}", examId);
                return StatusCode(500, new { error = "An error occurred while generating exam summary" });
            }
        }

        #region Helper Methods

        private async Task<ExamSubmissionDto> BuildExamSubmissionDto(
            string examId,
            McqSubmission? mcqSubmission,
            WrittenSubmission? writtenSubmission)
        {
            var studentId = mcqSubmission?.StudentId ?? writtenSubmission?.StudentId ?? "";
            var (totalScore, totalMaxScore) = CalculateTotalScore(mcqSubmission, writtenSubmission);
            var status = DetermineSubmissionStatus(mcqSubmission, writtenSubmission);
            var submissionType = DetermineSubmissionType(mcqSubmission, writtenSubmission);

            var latestTime = new[] { mcqSubmission?.SubmittedAt, writtenSubmission?.SubmittedAt }
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            return new ExamSubmissionDto
            {
                ExamId = examId,
                StudentId = studentId,
                SubmissionType = submissionType,
                McqSubmittedAt = mcqSubmission?.SubmittedAt,
                WrittenSubmittedAt = writtenSubmission?.SubmittedAt,
                LatestSubmissionTime = latestTime,
                TotalScore = totalScore,
                TotalMaxScore = totalMaxScore,
                Percentage = totalScore.HasValue && totalMaxScore.HasValue && totalMaxScore > 0
                    ? Math.Round((totalScore.Value / totalMaxScore.Value) * 100, 2)
                    : null,
                Status = status
            };
        }

        private async Task<ExamSubmissionDetailDto> BuildExamSubmissionDetailDto(
            GeneratedExamResponse exam,
            McqSubmission? mcqSubmission,
            WrittenSubmission? writtenSubmission)
        {
            var studentId = mcqSubmission?.StudentId ?? writtenSubmission?.StudentId ?? "";
            
            // Get subjective evaluations if written submission exists
            var subjectiveEvaluations = new List<SubjectiveEvaluationDetailDto>();
            double subjectiveScore = 0;
            double subjectiveTotalMarks = 0;

            if (writtenSubmission != null)
            {
                var evaluations = await _examRepository.GetSubjectiveEvaluationsAsync(writtenSubmission.WrittenSubmissionId);
                foreach (var eval in evaluations)
                {
                    subjectiveScore += eval.EarnedMarks;
                    subjectiveTotalMarks += eval.MaxMarks;

                    subjectiveEvaluations.Add(new SubjectiveEvaluationDetailDto
                    {
                        QuestionId = eval.QuestionId,
                        QuestionNumber = eval.QuestionNumber,
                        EarnedMarks = eval.EarnedMarks,
                        MaxMarks = eval.MaxMarks,
                        IsFullyCorrect = eval.IsFullyCorrect,
                        ExpectedAnswer = eval.ExpectedAnswer,
                        StudentAnswerEcho = eval.StudentAnswerEcho,
                        OverallFeedback = eval.OverallFeedback,
                        StepAnalysis = eval.StepAnalysis.Select(s => new StepAnalysisDto
                        {
                            Step = s.Step,
                            Description = s.Description,
                            IsCorrect = s.IsCorrect,
                            MarksAwarded = s.MarksAwarded,
                            MaxMarksForStep = s.MaxMarksForStep,
                            Feedback = s.Feedback
                        }).ToList()
                    });
                }
            }

            // Calculate overall scores
            int mcqScore = mcqSubmission?.Score ?? 0;
            int mcqTotalMarks = mcqSubmission?.TotalMarks ?? 0;
            double grandScore = mcqScore + subjectiveScore;
            double grandTotalMarks = mcqTotalMarks + subjectiveTotalMarks;
            double percentage = grandTotalMarks > 0 ? Math.Round((grandScore / grandTotalMarks) * 100, 2) : 0;

            return new ExamSubmissionDetailDto
            {
                ExamId = exam.ExamId ?? "",
                StudentId = studentId,
                ExamTitle = $"{exam.Subject} - {exam.Chapter}",
                Subject = exam.Subject ?? "",
                GradeLevel = exam.Grade,
                Chapter = exam.Chapter,
                HasMcqSubmission = mcqSubmission != null,
                McqSubmittedAt = mcqSubmission?.SubmittedAt,
                McqScore = mcqSubmission?.Score,
                McqTotalMarks = mcqSubmission?.TotalMarks,
                McqAnswers = mcqSubmission?.Answers.Select(a => new McqAnswerDetailDto
                {
                    QuestionId = a.QuestionId,
                    SelectedOption = a.SelectedOption,
                    CorrectAnswer = "", // This would need to be fetched from exam
                    IsCorrect = a.IsCorrect,
                    MarksAwarded = a.MarksAwarded
                }).ToList() ?? new List<McqAnswerDetailDto>(),
                HasWrittenSubmission = writtenSubmission != null,
                WrittenSubmittedAt = writtenSubmission?.SubmittedAt,
                WrittenSubmissionId = writtenSubmission?.WrittenSubmissionId,
                WrittenStatus = ConvertSubmissionStatus(writtenSubmission?.Status ?? Models.SubmissionStatus.PendingEvaluation),
                SubjectiveScore = subjectiveScore,
                SubjectiveTotalMarks = subjectiveTotalMarks,
                SubjectiveEvaluations = subjectiveEvaluations,
                GrandScore = grandScore,
                GrandTotalMarks = grandTotalMarks,
                Percentage = percentage,
                LetterGrade = CalculateGrade(percentage),
                Passed = percentage >= 35
            };
        }

        private async Task<StudentExamHistoryDto> BuildStudentExamHistoryDto(
            GeneratedExamResponse exam,
            McqSubmission? mcqSubmission,
            WrittenSubmission? writtenSubmission)
        {
            var (score, totalMarks) = CalculateTotalScore(mcqSubmission, writtenSubmission);
            var status = DetermineSubmissionStatus(mcqSubmission, writtenSubmission);
            var submissionType = DetermineSubmissionType(mcqSubmission, writtenSubmission);

            var attemptedAt = new[] { mcqSubmission?.SubmittedAt, writtenSubmission?.SubmittedAt }
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            return new StudentExamHistoryDto
            {
                ExamId = exam.ExamId ?? "",
                ExamTitle = $"{exam.Subject} - {exam.Chapter}",
                Subject = exam.Subject ?? "",
                GradeLevel = exam.Grade,
                Chapter = exam.Chapter,
                AttemptedAt = attemptedAt,
                Score = score,
                TotalMarks = totalMarks,
                Percentage = score.HasValue && totalMarks.HasValue && totalMarks > 0
                    ? Math.Round((score.Value / totalMarks.Value) * 100, 2)
                    : null,
                Status = status,
                SubmissionType = submissionType
            };
        }

        private (double? score, double? totalMarks) CalculateTotalScore(
            McqSubmission? mcqSubmission,
            WrittenSubmission? writtenSubmission)
        {
            double? score = null;
            double? totalMarks = null;

            if (mcqSubmission != null)
            {
                score = mcqSubmission.Score;
                totalMarks = mcqSubmission.TotalMarks;
            }

            // Note: Subjective scores would need to be calculated from evaluations
            // For now, only MCQ scores are included
            // In a full implementation, you'd fetch subjective evaluations here

            return (score, totalMarks);
        }

        private SubmissionStatusType DetermineSubmissionStatus(
            McqSubmission? mcqSubmission,
            WrittenSubmission? writtenSubmission)
        {
            if (mcqSubmission == null && writtenSubmission == null)
                return SubmissionStatusType.NotStarted;

            if (writtenSubmission != null)
            {
                return ConvertSubmissionStatus(writtenSubmission.Status);
            }

            if (mcqSubmission != null && writtenSubmission == null)
                return SubmissionStatusType.PartiallyCompleted;

            return SubmissionStatusType.Completed;
        }

        private SubmissionType DetermineSubmissionType(
            McqSubmission? mcqSubmission,
            WrittenSubmission? writtenSubmission)
        {
            if (mcqSubmission != null && writtenSubmission != null)
                return SubmissionType.Both;
            if (mcqSubmission != null)
                return SubmissionType.MCQOnly;
            if (writtenSubmission != null)
                return SubmissionType.WrittenOnly;
            return SubmissionType.None;
        }

        private SubmissionStatusType ConvertSubmissionStatus(Models.SubmissionStatus status)
        {
            return status switch
            {
                Models.SubmissionStatus.PendingEvaluation => SubmissionStatusType.PendingEvaluation,
                Models.SubmissionStatus.OcrProcessing => SubmissionStatusType.Evaluating,
                Models.SubmissionStatus.Evaluating => SubmissionStatusType.Evaluating,
                Models.SubmissionStatus.Completed => SubmissionStatusType.Completed,
                Models.SubmissionStatus.Failed => SubmissionStatusType.Failed,
                _ => SubmissionStatusType.NotStarted
            };
        }

        private string CalculateGrade(double percentage)
        {
            if (percentage >= 90) return "A+";
            if (percentage >= 80) return "A";
            if (percentage >= 70) return "B+";
            if (percentage >= 60) return "B";
            if (percentage >= 50) return "C";
            if (percentage >= 35) return "D";
            return "F";
        }

        #endregion
    }
}
