using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolAiChatbotBackend.Data;
using System.Text;
using Microsoft.OpenApi.Models;
using SchoolAiChatbotBackend.Services;

namespace SchoolAiChatbotBackend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Update CORS policy to allow frontend and any origin for development
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy => policy
                        .WithOrigins(
                            "https://nice-ocean-0bd32c110.3.azurestaticapps.net",
                            "http://localhost:3000",
                            "https://localhost:3000"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
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

            // Configure EF Core for Azure SQL Server
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                // Use SQL Server when connection string is available
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));
            }
            else
            {
                // Fallback to in-memory database if no connection string (for debugging)
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TempSchoolAiDb"));
                
                var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
                logger?.LogWarning("No database connection string found. Using in-memory database. Configure 'ConnectionStrings:DefaultConnection' for production.");
            }
// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty))
        };
    });

builder.Services.AddAuthorization();
// Register external services
builder.Services.AddScoped<SchoolAiChatbotBackend.Services.JwtService>();
builder.Services.AddScoped<SchoolAiChatbotBackend.Services.PineconeService>();
builder.Services.AddScoped<SchoolAiChatbotBackend.Services.FaqEmbeddingService>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Also add a simple file logger to persist logs to backend.log for debugging
// builder.Logging.AddProvider(new SchoolAiChatbotBackend.Logging.FileLoggerProvider(Path.Combine(builder.Environment.ContentRootPath, "backend.log")));

// Set default minimum log level to Information so request logs are emitted.
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register OpenAiChatService using a factory that reads config
builder.Services.AddScoped<OpenAiChatService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    
    // Try multiple configuration keys for OpenAI API key
    var apiKey = config["OpenAI:ApiKey"] ?? 
                 config["OpenAI__ApiKey"] ?? 
                 config["OPENAI_API_KEY"] ??
                 Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                 "YOUR_OPENAI_API_KEY";

    if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENAI_API_KEY")
        throw new InvalidOperationException("OpenAI ApiKey not found. Set OPENAI_API_KEY environment variable or OpenAI:ApiKey in configuration.");

    return new OpenAiChatService(apiKey);
});

// Register chat service implementation based on configuration flag 'UseClaude'
builder.Services.AddScoped<IChatService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var useClaude = bool.TryParse(config["UseClaude"], out var enabled) && enabled;
    if (useClaude)
    {
        return provider.GetRequiredService<ClaudeChatService>();
    }
    else
    {
        return provider.GetRequiredService<OpenAiChatService>();
    }
});

// Ensure ClaudeChatService is available for DI if requested
builder.Services.AddScoped<ClaudeChatService>();

var app = builder.Build();

// Log the connection string we're using so it's easy to verify at runtime
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
// var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
// startupLogger.LogInformation("Using DB connection: {Conn}", connStr);

// Add middleware to log incoming requests early in the pipeline so we capture all hits.
app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var origin = context.Request.Headers["Origin"].ToString();
    var referer = context.Request.Headers["Referer"].ToString();
    var userAgent = context.Request.Headers["User-Agent"].ToString();

    logger.LogInformation("Frontend hit: {Method} {Path} from {RemoteIp} Origin={Origin} Referer={Referer} UA={UserAgent}",
        context.Request.Method, context.Request.Path, ip, origin, referer, userAgent);

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Always allow CORS for all origins to handle frontend requests
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader() 
    .AllowAnyMethod());

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

            // Ensure database is created and seeded (only if we have a proper connection string)
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(dbConnectionString))
                    {
                        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
                        var logger = app.Services.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Database seeding completed successfully");
                    }
                    else
                    {
                        var logger = app.Services.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Skipping database seeding - no connection string configured");
                    }
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error during database seeding - application will continue without seeded data");
                }
            }

            // Add health check endpoints with database connection status
            app.MapGet("/health", (IConfiguration config) => 
            {
                var hasConnectionString = !string.IsNullOrEmpty(config.GetConnectionString("DefaultConnection"));
                return Results.Ok(new 
                { 
                    status = "healthy", 
                    timestamp = DateTime.UtcNow,
                    database = hasConnectionString ? "configured" : "not_configured"
                });
            });
            
            app.MapGet("/api/health", (IConfiguration config) => 
            {
                var hasConnectionString = !string.IsNullOrEmpty(config.GetConnectionString("DefaultConnection"));
                return Results.Ok(new 
                { 
                    status = "healthy", 
                    timestamp = DateTime.UtcNow, 
                    api = "v1",
                    database = hasConnectionString ? "configured" : "not_configured"
                });
            });
app.MapGet("/api/ping", () => Results.Ok("pong"));
app.MapGet("/", () => Results.Ok(new { message = "School AI Chatbot Backend is running with Azure SQL Server", version = "1.0.2", timestamp = DateTime.UtcNow }));

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
