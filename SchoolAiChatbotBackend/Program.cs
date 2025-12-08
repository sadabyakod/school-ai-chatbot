using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Middleware;
using System.Text;
using Microsoft.OpenApi.Models;
using SchoolAiChatbotBackend.Services;
using Serilog;
using Serilog.Events;

namespace SchoolAiChatbotBackend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog before creating the builder
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/app-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] [{MachineName}] [{ThreadId}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Starting School AI Chatbot Backend");

                var builder = WebApplication.CreateBuilder(args);

                // Use Serilog for logging
                builder.Host.UseSerilog();
                
                // CORS: Only allow your frontend in production
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontend",
                        policy => policy
                            .WithOrigins(
                                "https://proud-hill-07ee6991e3.azurestaticapps.net",
                                "https://nice-ocean-0bd32c110.3.azurestaticapps.net",
                                "http://localhost:5173",
                                "http://localhost:5174",
                                "http://localhost:5175"
                            )
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials());
                });
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524_288_000; // 500 MB for chunked uploads
    options.Limits.MinRequestBodyDataRate = null; // Disable minimum data rate for large uploads
    options.Limits.MinResponseDataRate = null;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // 10 minutes for large file uploads
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
    
    // Azure App Service will set the PORT environment variable
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port)); // Listen on Azure's expected port
});
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "School AI Chatbot API", 
        Version = "v1",
        Description = "API for School AI Chatbot with RAG, Chat History, Study Notes, and Adaptive Exam System",
        Contact = new OpenApiContact
        {
            Name = "School AI Support",
            Email = "support@schoolai.com"
        }
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

            // Configure EF Core for Azure SQL Server
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? builder.Configuration.GetConnectionString("SqlDb");

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
                logger?.LogWarning("No database connection string found. Using in-memory database. Configure 'ConnectionStrings:DefaultConnection' or 'ConnectionStrings:SqlDb' for production.");
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

// Register HttpClient for OpenAIService
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();

// Register core services
builder.Services.AddScoped<SchoolAiChatbotBackend.Services.JwtService>();

// Register Azure Functions migration services (SQL-based RAG)
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<SchoolAiChatbotBackend.Services.IChatHistoryService, SchoolAiChatbotBackend.Services.ChatHistoryService>();
builder.Services.AddScoped<SchoolAiChatbotBackend.Services.IRAGService, SchoolAiChatbotBackend.Services.RAGService>();
builder.Services.AddScoped<SchoolAiChatbotBackend.Services.IStudyNotesService, SchoolAiChatbotBackend.Services.StudyNotesService>();

// Register Exam System services
builder.Services.AddScoped<SchoolAiChatbotBackend.Features.Exams.IExamService, SchoolAiChatbotBackend.Features.Exams.ExamService>();

// Register Exam Submission services (New)
builder.Services.AddScoped<IExamRepository, InMemoryExamRepository>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMathOcrNormalizer, MathOcrNormalizer>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<ISubjectiveEvaluator, SubjectiveEvaluator>();

// Register Exam Storage service (Database-backed - persists exams to Azure SQL)
builder.Services.AddSingleton<IExamStorageService, DatabaseExamStorageService>();

// Register Subjective Rubric services (Step-based marking)
builder.Services.AddScoped<SchoolAiChatbotBackend.Repositories.ISubjectiveRubricRepository, SchoolAiChatbotBackend.Repositories.SubjectiveRubricRepository>();
builder.Services.AddScoped<ISubjectiveRubricService, SubjectiveRubricService>();

// Register global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
app.UseRequestLogging();

// Add global exception handling
app.UseExceptionHandler();

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


            // Enable Swagger in all environments
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "School AI Chatbot API v1");
                c.RoutePrefix = "swagger"; // Access at /swagger
                c.DocumentTitle = "School AI Chatbot API";
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            }
            else
            {
                app.UseCors("AllowFrontend");
            }

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
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
