using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SchoolAiChatbotBackend.Tests
{
    public class ChatControllerTests
    {
        private readonly Mock<IRAGService> _mockRagService;
        private readonly Mock<IChatHistoryService> _mockChatHistoryService;
        private readonly Mock<ILogger<ChatController>> _mockLogger;
        private readonly ChatController _controller;

        public ChatControllerTests()
        {
            _mockRagService = new Mock<IRAGService>();
            _mockChatHistoryService = new Mock<IChatHistoryService>();
            _mockLogger = new Mock<ILogger<ChatController>>();
            _controller = new ChatController(_mockRagService.Object, _mockChatHistoryService.Object, _mockLogger.Object);
            
            // Mock HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GetMostRecentSession_ReturnsSessionId_WhenSessionExists()
        {
            // Arrange
            string userId = "127.0.0.1"; // IP address from mocked HttpContext
            string expectedSessionId = "session-123";
            _mockChatHistoryService.Setup(s => s.GetMostRecentSessionAsync(userId))
                .ReturnsAsync(expectedSessionId);

            // Act
            var result = await _controller.GetMostRecentSession();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Access the anonymous type properties using reflection
            var statusProperty = okResult.Value.GetType().GetProperty("status");
            var sessionIdProperty = okResult.Value.GetType().GetProperty("sessionId");
            
            Assert.NotNull(statusProperty);
            Assert.NotNull(sessionIdProperty);
            Assert.Equal("success", statusProperty.GetValue(okResult.Value));
            Assert.Equal(expectedSessionId, sessionIdProperty.GetValue(okResult.Value));
        }

        [Fact]
        public async Task GetMostRecentSession_ReturnsNotFound_WhenNoSessionExists()
        {
            // Arrange
            string userId = "127.0.0.1"; // IP address from mocked HttpContext
            _mockChatHistoryService.Setup(s => s.GetMostRecentSessionAsync(userId))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _controller.GetMostRecentSession();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            
            // Access the anonymous type properties using reflection
            var statusProperty = notFoundResult.Value.GetType().GetProperty("status");
            var messageProperty = notFoundResult.Value.GetType().GetProperty("message");
            
            Assert.NotNull(statusProperty);
            Assert.NotNull(messageProperty);
            Assert.Equal("error", statusProperty.GetValue(notFoundResult.Value));
            Assert.Equal("No recent session found.", messageProperty.GetValue(notFoundResult.Value));
        }
    }
}
