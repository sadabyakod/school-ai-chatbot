using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SchoolAiChatbotBackend.Tests
{
    public class StudyNotesServiceTests
    {
        private readonly Mock<IRAGService> _mockRagService;
        private readonly Mock<IOpenAIService> _mockOpenAIService;
        private readonly Mock<ILogger<StudyNotesService>> _mockLogger;
        private readonly AppDbContext _dbContext;
        private readonly StudyNotesService _service;

        public StudyNotesServiceTests()
        {
            _mockRagService = new Mock<IRAGService>();
            _mockOpenAIService = new Mock<IOpenAIService>();
            _mockLogger = new Mock<ILogger<StudyNotesService>>();

            // Create in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new AppDbContext(options);

            _service = new StudyNotesService(
                _mockRagService.Object,
                _mockOpenAIService.Object,
                _dbContext,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task UpdateStudyNoteAsync_UpdatesContentAndTimestamp_WhenNoteExists()
        {
            // Arrange
            var userId = "test-user";
            var note = new StudyNote
            {
                UserId = userId,
                Topic = "Test Topic",
                GeneratedNotes = "Original content",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            var updatedContent = "Updated content";

            // Act
            var result = await _service.UpdateStudyNoteAsync(note.Id, userId, updatedContent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updatedContent, result.GeneratedNotes);
            Assert.NotNull(result.UpdatedAt);
            Assert.True(result.UpdatedAt > result.CreatedAt);
        }

        [Fact]
        public async Task UpdateStudyNoteAsync_ReturnsNull_WhenUserNotAuthorized()
        {
            // Arrange
            var note = new StudyNote
            {
                UserId = "user1",
                Topic = "Test Topic",
                GeneratedNotes = "Original content"
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateStudyNoteAsync(note.Id, "different-user", "New content");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ShareStudyNoteAsync_GeneratesShareTokenAndSetsShared_WhenNoteExists()
        {
            // Arrange
            var userId = "test-user";
            var note = new StudyNote
            {
                UserId = userId,
                Topic = "Test Topic",
                GeneratedNotes = "Content to share",
                IsShared = false
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var shareToken = await _service.ShareStudyNoteAsync(note.Id, userId);

            // Assert
            Assert.NotNull(shareToken);
            Assert.NotEmpty(shareToken);
            
            var updatedNote = await _dbContext.StudyNotes.FindAsync(note.Id);
            Assert.NotNull(updatedNote);
            Assert.True(updatedNote.IsShared);
            Assert.Equal(shareToken, updatedNote.ShareToken);
        }

        [Fact]
        public async Task ShareStudyNoteAsync_ReusesExistingToken_WhenAlreadyGenerated()
        {
            // Arrange
            var userId = "test-user";
            var existingToken = Guid.NewGuid().ToString("N");
            var note = new StudyNote
            {
                UserId = userId,
                Topic = "Test Topic",
                GeneratedNotes = "Content",
                ShareToken = existingToken,
                IsShared = false
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var shareToken = await _service.ShareStudyNoteAsync(note.Id, userId);

            // Assert
            Assert.Equal(existingToken, shareToken);
        }

        [Fact]
        public async Task ShareStudyNoteAsync_ReturnsNull_WhenUserNotAuthorized()
        {
            // Arrange
            var note = new StudyNote
            {
                UserId = "user1",
                Topic = "Test Topic",
                GeneratedNotes = "Content"
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.ShareStudyNoteAsync(note.Id, "different-user");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UnshareStudyNoteAsync_SetsIsSharedToFalse_WhenNoteExists()
        {
            // Arrange
            var userId = "test-user";
            var note = new StudyNote
            {
                UserId = userId,
                Topic = "Test Topic",
                GeneratedNotes = "Shared content",
                IsShared = true,
                ShareToken = Guid.NewGuid().ToString("N")
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UnshareStudyNoteAsync(note.Id, userId);

            // Assert
            Assert.True(result);
            
            var updatedNote = await _dbContext.StudyNotes.FindAsync(note.Id);
            Assert.NotNull(updatedNote);
            Assert.False(updatedNote.IsShared);
        }

        [Fact]
        public async Task UnshareStudyNoteAsync_ReturnsFalse_WhenUserNotAuthorized()
        {
            // Arrange
            var note = new StudyNote
            {
                UserId = "user1",
                Topic = "Test Topic",
                GeneratedNotes = "Content",
                IsShared = true
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UnshareStudyNoteAsync(note.Id, "different-user");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetSharedStudyNoteAsync_ReturnsNote_WhenTokenIsValidAndShared()
        {
            // Arrange
            var shareToken = Guid.NewGuid().ToString("N");
            var note = new StudyNote
            {
                UserId = "user1",
                Topic = "Shared Topic",
                GeneratedNotes = "Shared content",
                IsShared = true,
                ShareToken = shareToken
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSharedStudyNoteAsync(shareToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(note.Id, result.Id);
            Assert.Equal("Shared Topic", result.Topic);
        }

        [Fact]
        public async Task GetSharedStudyNoteAsync_ReturnsNull_WhenNoteIsNotShared()
        {
            // Arrange
            var shareToken = Guid.NewGuid().ToString("N");
            var note = new StudyNote
            {
                UserId = "user1",
                Topic = "Private Topic",
                GeneratedNotes = "Private content",
                IsShared = false,
                ShareToken = shareToken
            };
            _dbContext.StudyNotes.Add(note);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSharedStudyNoteAsync(shareToken);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSharedStudyNoteAsync_ReturnsNull_WhenTokenDoesNotExist()
        {
            // Arrange
            var nonExistentToken = Guid.NewGuid().ToString("N");

            // Act
            var result = await _service.GetSharedStudyNoteAsync(nonExistentToken);

            // Assert
            Assert.Null(result);
        }
    }
}
