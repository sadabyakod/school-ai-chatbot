using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.DTOs;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SchoolAiChatbotBackend.Tests
{
    /// <summary>
    /// Unit tests for ExamAnalyticsController
    /// Tests all analytics endpoints for exam submission review and reporting
    /// </summary>
    public class ExamAnalyticsControllerTests
    {
        private readonly Mock<IExamRepository> _mockExamRepository;
        private readonly Mock<IExamStorageService> _mockExamStorageService;
        private readonly Mock<ILogger<ExamAnalyticsController>> _mockLogger;
        private readonly ExamAnalyticsController _controller;

        public ExamAnalyticsControllerTests()
        {
            _mockExamRepository = new Mock<IExamRepository>();
            _mockExamStorageService = new Mock<IExamStorageService>();
            _mockLogger = new Mock<ILogger<ExamAnalyticsController>>();
            _controller = new ExamAnalyticsController(
                _mockExamRepository.Object,
                _mockExamStorageService.Object,
                _mockLogger.Object
            );
        }

        #region GetExamSubmissions Tests

        [Fact]
        public async Task GetExamSubmissions_ReturnsNotFound_WhenExamDoesNotExist()
        {
            // Arrange
            var examId = "non-existent-exam";
            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync((GeneratedExamResponse?)null);

            // Act
            var result = await _controller.GetExamSubmissions(examId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetExamSubmissions_ReturnsEmptyList_WhenNoSubmissions()
        {
            // Arrange
            var examId = "exam-123";
            var exam = CreateSampleExamResponse(examId);
            
            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync(exam);
            
            _mockExamRepository
                .Setup(r => r.GetAllSubmissionsByExamAsync(examId))
                .ReturnsAsync(new List<(McqSubmission?, WrittenSubmission?)>());

            // Act
            var result = await _controller.GetExamSubmissions(examId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedList = Assert.IsType<PaginatedListDto<ExamSubmissionDto>>(okResult.Value);
            Assert.Empty(paginatedList.Items);
            Assert.Equal(0, paginatedList.TotalCount);
        }

        [Fact]
        public async Task GetExamSubmissions_ReturnsPaginatedList_WithSubmissions()
        {
            // Arrange
            var examId = "exam-123";
            var exam = CreateSampleExamResponse(examId);
            var submissions = CreateSampleSubmissions(examId, 5);

            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync(exam);
            
            _mockExamRepository
                .Setup(r => r.GetAllSubmissionsByExamAsync(examId))
                .ReturnsAsync(submissions);

            // Act
            var result = await _controller.GetExamSubmissions(examId, page: 1, pageSize: 3);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedList = Assert.IsType<PaginatedListDto<ExamSubmissionDto>>(okResult.Value);
            Assert.Equal(3, paginatedList.Items.Count);
            Assert.Equal(5, paginatedList.TotalCount);
            Assert.Equal(1, paginatedList.Page);
            Assert.Equal(2, paginatedList.TotalPages);
        }

        #endregion

        #region GetStudentSubmissionDetail Tests

        [Fact]
        public async Task GetStudentSubmissionDetail_ReturnsNotFound_WhenExamDoesNotExist()
        {
            // Arrange
            var examId = "non-existent-exam";
            var studentId = "student-001";
            
            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync((GeneratedExamResponse?)null);

            // Act
            var result = await _controller.GetStudentSubmissionDetail(examId, studentId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetStudentSubmissionDetail_ReturnsNotFound_WhenSubmissionDoesNotExist()
        {
            // Arrange
            var examId = "exam-123";
            var studentId = "non-existent-student";
            var exam = CreateSampleExamResponse(examId);

            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync(exam);
            
            _mockExamRepository
                .Setup(r => r.GetMcqSubmissionAsync(examId, studentId))
                .ReturnsAsync((McqSubmission?)null);
            
            _mockExamRepository
                .Setup(r => r.GetWrittenSubmissionByExamAndStudentAsync(examId, studentId))
                .ReturnsAsync((WrittenSubmission?)null);

            // Act
            var result = await _controller.GetStudentSubmissionDetail(examId, studentId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetStudentSubmissionDetail_ReturnsDetailedSubmission_WithMcqOnly()
        {
            // Arrange
            var examId = "exam-123";
            var studentId = "student-001";
            var exam = CreateSampleExamResponse(examId);
            var mcqSubmission = CreateMcqSubmission(examId, studentId, score: 8);

            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync(exam);
            
            _mockExamRepository
                .Setup(r => r.GetMcqSubmissionAsync(examId, studentId))
                .ReturnsAsync(mcqSubmission);
            
            _mockExamRepository
                .Setup(r => r.GetWrittenSubmissionByExamAndStudentAsync(examId, studentId))
                .ReturnsAsync((WrittenSubmission?)null);

            // Act
            var result = await _controller.GetStudentSubmissionDetail(examId, studentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var detail = Assert.IsType<ExamSubmissionDetailDto>(okResult.Value);
            Assert.Equal(examId, detail.ExamId);
            Assert.Equal(studentId, detail.StudentId);
            Assert.True(detail.HasMcqSubmission);
            Assert.False(detail.HasWrittenSubmission);
            Assert.Equal(8, detail.McqScore);
        }

        #endregion

        #region GetStudentExamHistory Tests

        [Fact]
        public async Task GetStudentExamHistory_ReturnsEmptyList_WhenNoSubmissions()
        {
            // Arrange
            var studentId = "student-001";
            
            _mockExamRepository
                .Setup(r => r.GetAllMcqSubmissionsByStudentAsync(studentId))
                .ReturnsAsync(new List<McqSubmission>());
            
            _mockExamRepository
                .Setup(r => r.GetAllWrittenSubmissionsByStudentAsync(studentId))
                .ReturnsAsync(new List<WrittenSubmission>());

            // Act
            var result = await _controller.GetStudentExamHistory(studentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedList = Assert.IsType<PaginatedListDto<StudentExamHistoryDto>>(okResult.Value);
            Assert.Empty(paginatedList.Items);
        }

        [Fact]
        public async Task GetStudentExamHistory_ReturnsPaginatedHistory_WithMultipleExams()
        {
            // Arrange
            var studentId = "student-001";
            var mcqSubmissions = new List<McqSubmission>
            {
                CreateMcqSubmission("exam-1", studentId, score: 8),
                CreateMcqSubmission("exam-2", studentId, score: 7)
            };
            var writtenSubmissions = new List<WrittenSubmission>
            {
                CreateWrittenSubmission("exam-1", studentId)
            };

            // Setup exam mocks
            _mockExamStorageService
                .Setup(s => s.GetExamAsync("exam-1"))
                .ReturnsAsync(CreateSampleExamResponse("exam-1"));
            _mockExamStorageService
                .Setup(s => s.GetExamAsync("exam-2"))
                .ReturnsAsync(CreateSampleExamResponse("exam-2"));

            // Setup evaluation mocks
            _mockExamRepository
                .Setup(r => r.GetSubjectiveEvaluationsAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateSubjectiveEvaluations("written-sub-1"));

            _mockExamRepository
                .Setup(r => r.GetAllMcqSubmissionsByStudentAsync(studentId))
                .ReturnsAsync(mcqSubmissions);
            
            _mockExamRepository
                .Setup(r => r.GetAllWrittenSubmissionsByStudentAsync(studentId))
                .ReturnsAsync(writtenSubmissions);

            // Act
            var result = await _controller.GetStudentExamHistory(studentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedList = Assert.IsType<PaginatedListDto<StudentExamHistoryDto>>(okResult.Value);
            Assert.Equal(2, paginatedList.Items.Count);
        }

        #endregion

        #region GetExamSummary Tests

        [Fact]
        public async Task GetExamSummary_ReturnsNotFound_WhenExamDoesNotExist()
        {
            // Arrange
            var examId = "non-existent-exam";
            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync((GeneratedExamResponse?)null);

            // Act
            var result = await _controller.GetExamSummary(examId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetExamSummary_ReturnsZeroMetrics_WhenNoSubmissions()
        {
            // Arrange
            var examId = "exam-123";
            var exam = CreateSampleExamResponse(examId);

            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync(exam);
            
            _mockExamRepository
                .Setup(r => r.GetAllSubmissionsByExamAsync(examId))
                .ReturnsAsync(new List<(McqSubmission?, WrittenSubmission?)>());

            // Act
            var result = await _controller.GetExamSummary(examId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var summary = Assert.IsType<ExamSummaryDto>(okResult.Value);
            Assert.Equal(examId, summary.ExamId);
            Assert.Equal(0, summary.TotalSubmissions);
            Assert.Equal(0, summary.CompletedSubmissions);
            Assert.Null(summary.AverageScore);
        }

        [Fact]
        public async Task GetExamSummary_CalculatesCorrectMetrics_WithSubmissions()
        {
            // Arrange
            var examId = "exam-123";
            var exam = CreateSampleExamResponse(examId);
            var submissions = new List<(McqSubmission?, WrittenSubmission?)>
            {
                (CreateMcqSubmission(examId, "student-1", score: 8), null),
                (CreateMcqSubmission(examId, "student-2", score: 10), null),
                (CreateMcqSubmission(examId, "student-3", score: 6), null)
            };

            _mockExamStorageService
                .Setup(s => s.GetExamAsync(examId))
                .ReturnsAsync(exam);
            
            _mockExamRepository
                .Setup(r => r.GetAllSubmissionsByExamAsync(examId))
                .ReturnsAsync(submissions);

            // Act
            var result = await _controller.GetExamSummary(examId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var summary = Assert.IsType<ExamSummaryDto>(okResult.Value);
            Assert.Equal(3, summary.TotalSubmissions);
            Assert.Equal(3, summary.CompletedSubmissions);
            Assert.Equal(8.0, summary.AverageScore);
            Assert.Equal(6.0, summary.MinScore);
            Assert.Equal(10.0, summary.MaxScore);
        }

        #endregion

        #region Helper Methods

        private GeneratedExamResponse CreateSampleExamResponse(string examId)
        {
            return new GeneratedExamResponse
            {
                ExamId = examId,
                Subject = "Mathematics",
                Grade = "2nd PUC",
                Chapter = "Calculus",
                TotalMarks = 100,
                Duration = 180
            };
        }

        private List<(McqSubmission?, WrittenSubmission?)> CreateSampleSubmissions(string examId, int count)
        {
            var submissions = new List<(McqSubmission?, WrittenSubmission?)>();
            for (int i = 1; i <= count; i++)
            {
                submissions.Add((CreateMcqSubmission(examId, $"student-{i:D3}", score: 5 + i), null));
            }
            return submissions;
        }

        private McqSubmission CreateMcqSubmission(string examId, string studentId, int score = 8)
        {
            return new McqSubmission
            {
                ExamId = examId,
                StudentId = studentId,
                Answers = new List<McqAnswer>
                {
                    new McqAnswer 
                    { 
                        QuestionId = "q1", 
                        SelectedOption = "4", 
                        IsCorrect = true, 
                        MarksAwarded = 1 
                    }
                },
                Score = score,
                TotalMarks = 10,
                SubmittedAt = DateTime.UtcNow.AddHours(-1)
            };
        }

        private WrittenSubmission CreateWrittenSubmission(string examId, string studentId, SubmissionStatus status = SubmissionStatus.Completed)
        {
            return new WrittenSubmission
            {
                WrittenSubmissionId = $"written-{examId}-{studentId}",
                ExamId = examId,
                StudentId = studentId,
                Status = status,
                SubmittedAt = DateTime.UtcNow.AddHours(-2)
            };
        }

        private List<SubjectiveEvaluationResult> CreateSubjectiveEvaluations(string writtenSubmissionId)
        {
            return new List<SubjectiveEvaluationResult>
            {
                new SubjectiveEvaluationResult
                {
                    WrittenSubmissionId = writtenSubmissionId,
                    QuestionNumber = 1,
                    EarnedMarks = 8.5,
                    MaxMarks = 10,
                    OverallFeedback = "Good answer",
                    StepAnalysis = new List<StepAnalysis>
                    {
                        new StepAnalysis 
                        { 
                            Step = 1, 
                            Description = "Introduction", 
                            MarksAwarded = 2, 
                            MaxMarksForStep = 2 
                        }
                    }
                }
            };
        }

        #endregion
    }
}
