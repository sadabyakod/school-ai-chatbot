using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolAiChatbotBackend.Data;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Text.Json;
// using SchoolAiChatbotBackend.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
// Update CORS policy to allow any origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50_000_000; // 50 MB
    options.ListenAnyIP(5001); // Listen on all network interfaces
});
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "School AI Chatbot API", Version = "v1" });
});

// Configure EF Core provider selection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";

if (string.IsNullOrEmpty(connectionString))
{
    // Fallback to Azure SQL Server connection string if not configured
    connectionString = "Server=school-chatbot-sql-10271900.database.windows.net;Database=SchoolAiChatbotDb;User Id=schooladmin;Password=SchoolAI123!@#;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
}

if (dbProvider == "MySql")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)) // Adjust MySQL version as needed
        ));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}


// Configure JWT authentication with robust key handling
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = Environment.GetEnvironmentVariable("JWT__SecretKey");
}

// If we still don't have a valid JWT key, use a fallback
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    jwtKey = "default-super-secret-jwt-key-for-development-only-minimum-32-characters-long";
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false
    };
});

builder.Services.AddAuthorization();
// Temporarily comment out external services for debugging
// builder.Services.AddScoped<SchoolAiChatbotBackend.Services.JwtService>();
// builder.Services.AddScoped<SchoolAiChatbotBackend.Services.PineconeService>();
// builder.Services.AddScoped<SchoolAiChatbotBackend.Services.FaqEmbeddingService>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Also add a simple file logger to persist logs to backend.log for debugging
// builder.Logging.AddProvider(new SchoolAiChatbotBackend.Logging.FileLoggerProvider(Path.Combine(builder.Environment.ContentRootPath, "backend.log")));

// Set default minimum log level to Information so request logs are emitted.
// builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register chat service implementation based on configuration flag 'UseClaude'
// Temporarily comment out chat services for debugging
// builder.Services.AddScoped<IChatService>(provider =>
// {
//     var config = provider.GetRequiredService<IConfiguration>();
//     var useClaude = bool.TryParse(config["UseClaude"], out var enabled) && enabled;
//     if (useClaude)
//     {
//         return provider.GetRequiredService<ClaudeChatService>();
//     }
//     else
//     {
//         var apiKey = config["OpenAI:ApiKey"] ?? "YOUR_OPENAI_API_KEY";
//         return new OpenAiChatService(apiKey);
//     }
// });
// 
// // Ensure ClaudeChatService is available for DI if requested
// builder.Services.AddScoped<ClaudeChatService>();

var app = builder.Build();

// Enable detailed error pages for debugging 500 errors
app.UseDeveloperExceptionPage();

// Add global exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("GlobalExceptionHandler");
        logger?.LogError(ex, "Unhandled exception in request {Method} {Path}", context.Request.Method, context.Request.Path);
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var errorDetails = new
        {
            error = "Internal Server Error",
            message = ex.Message,
            type = ex.GetType().Name,
            stackTrace = ex.StackTrace?.Split('\n').Take(10).ToArray(),
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorDetails));
    }
});

// Log the connection string we're using so it's easy to verify at runtime
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
// var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
// startupLogger.LogInformation("Using DB connection: {Conn}", connStr);

// Add middleware to log incoming requests early in the pipeline so we capture all hits.
// Temporarily commented out to debug 500 errors
// app.Use(async (context, next) =>
// {
//     var logger = app.Services.GetRequiredService<ILogger<Program>>();
//     var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
//     var origin = context.Request.Headers["Origin"].ToString();
//     var referer = context.Request.Headers["Referer"].ToString();
//     var userAgent = context.Request.Headers["User-Agent"].ToString();

//     logger.LogInformation("Frontend hit: {Method} {Path} from {RemoteIp} Origin={Origin} Referer={Referer} UA={UserAgent}",
//         context.Request.Method, context.Request.Path, ip, origin, referer, userAgent);

//     await next();
// });

// Enable Swagger in all environments for debugging
app.UseSwagger();
app.UseSwaggerUI();



app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
// Temporarily disable authentication to debug 500 errors
// app.UseAuthentication();
// app.UseAuthorization();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        // Apply any pending migrations
        context.Database.Migrate();
        
        // Seed data if database is empty
        if (!context.Faqs.Any())
        {
            context.Faqs.AddRange(
                new SchoolAiChatbotBackend.Models.Faq
                {
                    Question = "What are the school hours?",
                    Answer = "School hours are Monday-Friday 8:00 AM to 3:00 PM.",
                    Category = "General",
                    CreatedAt = DateTime.UtcNow
                },
                new SchoolAiChatbotBackend.Models.Faq
                {
                    Question = "How do I contact the school?", 
                    Answer = "You can contact us at (555) 123-4567 or email info@school.edu",
                    Category = "Contact",
                    CreatedAt = DateTime.UtcNow
                },
                new SchoolAiChatbotBackend.Models.Faq
                {
                    Question = "What is the homework policy?",
                    Answer = "Homework should take approximately 10 minutes per grade level (e.g., 3rd grade = 30 minutes).",
                    Category = "Academic", 
                    CreatedAt = DateTime.UtcNow
                }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // Log database connection errors but don't crash the app
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}

// Add comprehensive diagnostic endpoints
app.MapGet("/health", () => "healthy");
app.MapGet("/api/health", () => "api-healthy");
app.MapGet("/api/ping", () => "pong");
app.MapGet("/", () => "School AI Chatbot Backend is running");

// Test JSON serialization
app.MapGet("/api/test-json", () => Results.Json(new { message = "JSON test works", timestamp = DateTime.UtcNow }));

// Test service provider
app.MapGet("/api/test-services", (IServiceProvider services) => 
{
    var loggerFactory = services.GetService<ILoggerFactory>();
    var configuration = services.GetService<IConfiguration>();
    
    return Results.Json(new { 
        hasLoggerFactory = loggerFactory != null,
        hasConfiguration = configuration != null,
        serviceCount = services.GetType().Name,
        timestamp = DateTime.UtcNow
    });
});

// Test configuration access without injection
app.MapGet("/api/test-config", () => 
{
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
    var hasJwt = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT__SecretKey"));
    
    return Results.Json(new { 
        environment = env,
        hasJwtEnvVar = hasJwt,
        timestamp = DateTime.UtcNow,
        message = "Config test without injection"
    });
});

app.MapControllers();

app.Run();

public partial class Program { }
