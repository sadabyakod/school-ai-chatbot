using System.Diagnostics;

namespace SchoolAiChatbotBackend.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses with timing
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = context.TraceIdentifier;

            try
            {
                // Log request
                _logger.LogInformation(
                    "HTTP Request: {RequestId} {Method} {Path} | IP: {IP}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);

                // Call the next middleware
                await _next(context);

                stopwatch.Stop();

                // Log response
                var logLevel = context.Response.StatusCode >= 400
                    ? LogLevel.Warning
                    : LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "HTTP Response: {RequestId} {Method} {Path} | Status: {StatusCode} | Duration: {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "HTTP Request Failed: {RequestId} {Method} {Path} | Duration: {ElapsedMs}ms | Error: {Error}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <summary>
    /// Extension method for registering RequestLoggingMiddleware
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
