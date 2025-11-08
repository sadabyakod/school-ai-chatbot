using Microsoft.EntityFrameworkCore;
using ImageAPI.Data;
using ImageAPI.TestImageGenerator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Add Entity Framework with In-Memory Database for demo
builder.Services.AddDbContext<ImageDbContext>(options =>
    options.UseInMemoryDatabase("InMemoryImageDB"));

// Add CORS for WinForms client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Image API v1");
    });
    app.MapOpenApi();
}

// Enable CORS
app.UseCors();

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Generate test images on startup (development only)
if (app.Environment.IsDevelopment())
{
    var testImagesPath = Path.Combine(app.Environment.ContentRootPath, "TestImages");
    TestImageGenerator.GenerateTestImages(testImagesPath);
    
    // Update database with test image paths
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ImageDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Update sample records with actual test image paths
    var sampleImages = await context.Images.ToListAsync();
    if (sampleImages.Count >= 2)
    {
        sampleImages[0].ImagePath = Path.Combine(testImagesPath, "test-red.png");
        sampleImages[0].ImageName = "Test Red Rectangle";
        sampleImages[0].ContentType = "image/png";
        
        if (sampleImages.Count > 1)
        {
            sampleImages[1].ImagePath = Path.Combine(testImagesPath, "test-blue.png");
            sampleImages[1].ImageName = "Test Blue Circle";
            sampleImages[1].ContentType = "image/png";
        }
        
        await context.SaveChangesAsync();
    }
}

// Serve test HTML file
app.MapGet("/test", async (IWebHostEnvironment env) =>
{
    var htmlPath = Path.Combine(env.ContentRootPath, "Documentation", "test-base64.html");
    if (File.Exists(htmlPath))
    {
        var html = await File.ReadAllTextAsync(htmlPath);
        return Results.Content(html, "text/html");
    }
    return Results.NotFound("Test page not found");
});

// Root endpoint with API information
app.MapGet("/", () => new 
{
    name = "Image API",
    version = "1.0.0",
    description = "API for serving images from SQL Server to .NET Framework WinForms",
    endpoints = new
    {
        // Legacy endpoints (for backward compatibility)
        images = "/api/images",
        imageById = "/api/images/{id}",
        downloadImage = "/api/images/{id}/download",
        imageBase64 = "/api/images/{id}/base64",
        imageBase64Raw = "/api/images/{id}/base64raw",
        allImagesWithBase64 = "/api/images/all-base64",
        searchImages = "/api/images/search?query={query}",
        
        // New Inventory_Image table endpoints
        inventoryImages = "/api/inventory",
        inventoryByItem = "/api/inventory/item/{itemNum}",
        inventoryByItemAndStore = "/api/inventory/item/{itemNum}/store/{storeId}",
        inventoryDownload = "/api/inventory/{itemNum}/{storeId}/download?location={imageLocation}",
        inventoryBase64 = "/api/inventory/{itemNum}/{storeId}/base64?location={imageLocation}",
        inventoryBase64Raw = "/api/inventory/{itemNum}/{storeId}/base64raw?location={imageLocation}",
        inventoryAllBase64 = "/api/inventory/all-base64",
        inventorySearch = "/api/inventory/search?itemNum={itemNum}&storeId={storeId}",
        
        // Utility endpoints
        health = "/health",
        testPage = "/test",
        openapi = "/openapi/v1.json"
    }
});

app.Run();
