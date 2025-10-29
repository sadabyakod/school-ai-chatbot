using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolAiChatbotBackend.Data;
using System.Text;
using Microsoft.OpenApi.Models;
using SchoolAiChatbotBackend.Services;

var builder = WebApplication.CreateBuilder(args);
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

// Configure EF Core for Azure SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' is required for Azure SQL Server.");
}

// Always use SQL Server for production Azure deployment
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));


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



app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database seeding");
    }
}

// Add simple health check endpoints that don't depend on any services
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow, api = "v1" }));
app.MapGet("/api/ping", () => Results.Ok("pong"));
app.MapGet("/", () => Results.Ok(new { message = "School AI Chatbot Backend is running with Azure SQL Server", version = "1.0.1", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();

public partial class Program { }
