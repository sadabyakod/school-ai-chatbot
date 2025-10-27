using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "School AI Chatbot API", 
        Version = "v1",
        Description = "A comprehensive school chatbot API with AI integration"
    });
    
    // Add JWT support in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // For local development, use in-memory database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("SchoolAiChatbot"));
}
else
{
    // For production, use Azure SQL Database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));
}

// Configure JWT Authentication
// Prefer explicit Jwt:Key, fall back to environment variable JWT__SecretKey, then to a safe default.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = builder.Configuration["JWT__SecretKey"];
}
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = "default-super-secret-jwt-key-for-development-only";
}
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Register application services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IChatService, OpenAiChatService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "School AI Chatbot API v1");
        c.RoutePrefix = "swagger";  // Serve Swagger UI at /swagger
    });
}

// Create database and seed data if needed
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        // Create database if it doesn't exist
        await context.Database.EnsureCreatedAsync();
        
        // Seed initial data if database is empty
        if (!await context.Users.AnyAsync())
        {
            await SeedDatabase(context);
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating/seeding the database");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/", () => new { 
    Status = "Healthy", 
    Service = "School AI Chatbot API", 
    Version = "1.0.0",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
});

app.MapGet("/health", async (AppDbContext context) => 
{
    try
    {
        await context.Database.CanConnectAsync();
        return Results.Ok(new { 
            Status = "Healthy", 
            Database = "Connected",
            Timestamp = DateTime.UtcNow 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(new { 
            Status = "Unhealthy", 
            Database = "Disconnected",
            Error = ex.Message,
            Timestamp = DateTime.UtcNow 
        }.ToString());
    }
});

app.Run();

// Helper method to seed initial data
static async Task SeedDatabase(AppDbContext context)
{
    // Add default school
    var school = new SchoolAiChatbotBackend.Models.School
    {
        Name = "Demo School",
        Address = "123 Education St, Learning City, LC 12345",
        PhoneNumber = "(555) 123-4567",
        Email = "info@demoschool.edu",
        Website = "https://demoschool.edu"
    };
    context.Schools.Add(school);
    await context.SaveChangesAsync();

    // Add sample FAQs
    var faqs = new[]
    {
        new SchoolAiChatbotBackend.Models.Faq
        {
            Question = "What are the school hours?",
            Answer = "School hours are Monday-Friday 8:00 AM to 3:00 PM.",
            Category = "General",
            SchoolId = school.Id
        },
        new SchoolAiChatbotBackend.Models.Faq
        {
            Question = "How do I contact the school?",
            Answer = "You can contact us at (555) 123-4567 or email info@demoschool.edu",
            Category = "Contact",
            SchoolId = school.Id
        },
        new SchoolAiChatbotBackend.Models.Faq
        {
            Question = "What is the homework policy?",
            Answer = "Homework should take approximately 10 minutes per grade level (e.g., 3rd grade = 30 minutes).",
            Category = "Academic",
            SchoolId = school.Id
        },
        new SchoolAiChatbotBackend.Models.Faq
        {
            Question = "When is the next parent-teacher conference?",
            Answer = "Parent-teacher conferences are scheduled for November 15-16, 2024. Please sign up through the school portal.",
            Category = "Events",
            SchoolId = school.Id
        },
        new SchoolAiChatbotBackend.Models.Faq
        {
            Question = "What is the dress code policy?",
            Answer = "Students should wear appropriate school attire. No offensive language or images on clothing. Closed-toe shoes required.",
            Category = "Policies",
            SchoolId = school.Id
        }
    };

    context.Faqs.AddRange(faqs);
    await context.SaveChangesAsync();
}