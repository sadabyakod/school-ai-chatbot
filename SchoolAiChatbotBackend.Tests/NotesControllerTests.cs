using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SchoolAiChatbotBackend.Tests
{
    public class NotesControllerTests
    {
        private readonly Mock<IStudyNotesService> _mockNotesService;
        private readonly Mock<ILogger<NotesController>> _mockLogger;
        private readonly NotesController _controller;

        public NotesControllerTests()
        {
            _mockNotesService = new Mock<IStudyNotesService>();
            _mockLogger = new Mock<ILogger<NotesController>>();
            _controller = new NotesController(_mockNotesService.Object, _mockLogger.Object);

            // Mock HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task UpdateNote_ReturnsOk_WhenUpdateSuccessful()
        {
            // Arrange
            var noteId = 1;
            var updatedNote = new StudyNote
            {
                Id = noteId,
                Topic = "Test Topic",
                GeneratedNotes = "Updated content",
                UpdatedAt = DateTime.UtcNow
            };
            
            _mockNotesService
                .Setup(s => s.UpdateStudyNoteAsync(noteId, It.IsAny<string>(), "Updated content"))
                .ReturnsAsync(updatedNote);

            var request = new UpdateNoteRequest { Content = "Updated content" };

            // Act
            var result = await _controller.UpdateNote(noteId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            var statusProperty = okResult.Value.GetType().GetProperty("status");
            Assert.NotNull(statusProperty);
            Assert.Equal("success", statusProperty.GetValue(okResult.Value));
        }

        [Fact]
        public async Task UpdateNote_ReturnsNotFound_WhenNoteDoesNotExist()
        {
            // Arrange
            var noteId = 999;
            _mockNotesService
                .Setup(s => s.UpdateStudyNoteAsync(noteId, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((StudyNote?)null);

            var request = new UpdateNoteRequest { Content = "Updated content" };

            // Act
            var result = await _controller.UpdateNote(noteId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task ShareNote_ReturnsOk_WithShareToken_WhenSuccessful()
        {
            // Arrange
            var noteId = 1;
            var shareToken = Guid.NewGuid().ToString("N");
            
            _mockNotesService
                .Setup(s => s.ShareStudyNoteAsync(noteId, It.IsAny<string>()))
                .ReturnsAsync(shareToken);

            // Act
            var result = await _controller.ShareNote(noteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            var statusProperty = okResult.Value.GetType().GetProperty("status");
            var shareTokenProperty = okResult.Value.GetType().GetProperty("shareToken");
            
            Assert.NotNull(statusProperty);
            Assert.NotNull(shareTokenProperty);
            Assert.Equal("success", statusProperty.GetValue(okResult.Value));
            Assert.Equal(shareToken, shareTokenProperty.GetValue(okResult.Value));
        }

        [Fact]
        public async Task ShareNote_ReturnsNotFound_WhenNoteDoesNotExist()
        {
            // Arrange
            var noteId = 999;
            _mockNotesService
                .Setup(s => s.ShareStudyNoteAsync(noteId, It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _controller.ShareNote(noteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UnshareNote_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var noteId = 1;
            _mockNotesService
                .Setup(s => s.UnshareStudyNoteAsync(noteId, It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UnshareNote(noteId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            var statusProperty = okResult.Value.GetType().GetProperty("status");
            Assert.NotNull(statusProperty);
            Assert.Equal("success", statusProperty.GetValue(okResult.Value));
        }

        [Fact]
        public async Task UnshareNote_ReturnsNotFound_WhenNoteDoesNotExist()
        {
            // Arrange
            var noteId = 999;
            _mockNotesService
                .Setup(s => s.UnshareStudyNoteAsync(noteId, It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UnshareNote(noteId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetSharedNote_ReturnsOk_WhenSharedNoteExists()
        {
            // Arrange
            var shareToken = Guid.NewGuid().ToString("N");
            var sharedNote = new StudyNote
            {
                Id = 1,
                Topic = "Shared Topic",
                GeneratedNotes = "Shared content",
                IsShared = true,
                ShareToken = shareToken
            };
            
            _mockNotesService
                .Setup(s => s.GetSharedStudyNoteAsync(shareToken))
                .ReturnsAsync(sharedNote);

            // Act
            var result = await _controller.GetSharedNote(shareToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            var statusProperty = okResult.Value.GetType().GetProperty("status");
            Assert.NotNull(statusProperty);
            Assert.Equal("success", statusProperty.GetValue(okResult.Value));
        }

        [Fact]
        public async Task GetSharedNote_ReturnsNotFound_WhenTokenInvalid()
        {
            // Arrange
            var invalidToken = "invalid-token";
            _mockNotesService
                .Setup(s => s.GetSharedStudyNoteAsync(invalidToken))
                .ReturnsAsync((StudyNote?)null);

            // Act
            var result = await _controller.GetSharedNote(invalidToken);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }
    }
}
