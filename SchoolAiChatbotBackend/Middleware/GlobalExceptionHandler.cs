using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace SchoolAiChatbotBackend.Middleware
{
    /// <summary>
    /// Global exception handler middleware for production-ready error handling
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Exception occurred: {Message} | Path: {Path} | Method: {Method}",
                exception.Message,
                httpContext.Request.Path,
                httpContext.Request.Method);

            var problemDetails = CreateProblemDetails(httpContext, exception);

            httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(problemDetails),
                cancellationToken);

            return true;
        }

        private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
        {
            var statusCode = GetStatusCode(exception);
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(exception),
                Detail = GetDetail(exception),
                Instance = context.Request.Path,
                Type = GetTypeUrl(statusCode)
            };

            // Add stack trace only in development
            if (_environment.IsDevelopment())
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
            }

            // Add additional context
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            return problemDetails;
        }

        private static int GetStatusCode(Exception exception) => exception switch
        {
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            _ => StatusCodes.Status500InternalServerError
        };

        private static string GetTitle(Exception exception) => exception switch
        {
            ArgumentNullException => "Bad Request",
            ArgumentException => "Bad Request",
            UnauthorizedAccessException => "Unauthorized",
            InvalidOperationException => "Bad Request",
            KeyNotFoundException => "Not Found",
            NotImplementedException => "Not Implemented",
            TimeoutException => "Request Timeout",
            _ => "Internal Server Error"
        };

        private string GetDetail(Exception exception)
        {
            // In production, don't expose sensitive error details
            if (!_environment.IsDevelopment())
            {
                return exception switch
                {
                    ArgumentNullException => "A required parameter was not provided.",
                    ArgumentException => "One or more parameters are invalid.",
                    UnauthorizedAccessException => "You are not authorized to access this resource.",
                    KeyNotFoundException => "The requested resource was not found.",
                    TimeoutException => "The request took too long to process.",
                    _ => "An unexpected error occurred. Please try again later."
                };
            }

            return exception.Message;
        }

        private static string GetTypeUrl(int statusCode)
        {
            return statusCode switch
            {
                400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
                404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                408 => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
                500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                501 => "https://tools.ietf.org/html/rfc7231#section-6.6.2",
                _ => "https://tools.ietf.org/html/rfc7231"
            };
        }
    }
}
