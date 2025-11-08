# Image API for WinForms Integration

## Overview
This API provides endpoints to serve images stored in SQL Server to .NET Framework 4.8 WinForms applications.

## API Endpoints

### 1. Get All Images
- **URL**: `GET /api/images`
- **Description**: Retrieve all image records with metadata
- **Response**: JSON array with image information including download URLs

### 2. Get Specific Image
- **URL**: `GET /api/images/{id}`
- **Description**: Get metadata for a specific image by ID
- **Parameters**: `id` (integer) - Image ID
- **Response**: JSON object with image details

### 3. Download Image File
- **URL**: `GET /api/images/{id}/download`
- **Description**: Download the actual image file
- **Parameters**: `id` (integer) - Image ID
- **Response**: Binary file with appropriate content-type header

### 4. Get Image as Base64
- **URL**: `GET /api/images/{id}/base64`
- **Description**: Get image as Base64 data URI (perfect for WinForms)
- **Parameters**: `id` (integer) - Image ID
- **Response**: JSON object with Base64 data URI string

### 5. Search Images
- **URL**: `GET /api/images/search?query={query}`
- **Description**: Search images by name or description
- **Parameters**: `query` (string) - Search term
- **Response**: JSON array with matching images

### 6. Health Check
- **URL**: `GET /health`
- **Description**: API health status
- **Response**: JSON object with status and timestamp

## Database Schema

### Images Table
```sql
CREATE TABLE Images (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ImagePath nvarchar(500) NOT NULL,
    ImageName nvarchar(255),
    Description nvarchar(500),
    CreatedDate datetime2 DEFAULT GETUTCDATE(),
    ModifiedDate datetime2,
    ContentType nvarchar(50),
    FileSize bigint
);
```

## Configuration

### Connection String
Update `appsettings.json` or `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string here"
  }
}
```

### CORS
CORS is enabled for all origins to support WinForms HttpClient calls.

## WinForms Integration Examples

### Using HttpClient (Recommended for .NET Framework 4.8)

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

public class ImageApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ImageApiClient(string baseUrl = "https://localhost:7000")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    // Get all images
    public async Task<List<ImageInfo>> GetAllImagesAsync()
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/api/images");
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<ImageInfo>>>(response);
        return apiResponse.Data ?? new List<ImageInfo>();
    }

    // Download image as byte array
    public async Task<byte[]> DownloadImageAsync(int imageId)
    {
        return await _httpClient.GetByteArrayAsync($"{_baseUrl}/api/images/{imageId}/download");
    }

    // Get image as Base64 for direct display
    public async Task<string> GetImageBase64Async(int imageId)
    {
        var response = await _httpClient.GetStringAsync($"{_baseUrl}/api/images/{imageId}/base64");
        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<string>>(response);
        return apiResponse.Data ?? string.Empty;
    }

    // Load image into PictureBox
    public async Task LoadImageToPictureBox(PictureBox pictureBox, int imageId)
    {
        try
        {
            var imageBytes = await DownloadImageAsync(imageId);
            using (var ms = new MemoryStream(imageBytes))
            {
                pictureBox.Image = Image.FromStream(ms);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading image: {ex.Message}", "Error", 
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

// Data Transfer Objects
public class ImageInfo
{
    public int Id { get; set; }
    public string ImageName { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public string ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool FileExists { get; set; }
    public string DownloadUrl { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Example WinForms Usage

```csharp
public partial class Form1 : Form
{
    private ImageApiClient _apiClient;

    public Form1()
    {
        InitializeComponent();
        _apiClient = new ImageApiClient("https://localhost:7000");
    }

    private async void LoadImagesButton_Click(object sender, EventArgs e)
    {
        try
        {
            var images = await _apiClient.GetAllImagesAsync();
            
            // Populate a ListBox or ComboBox
            imageListBox.DataSource = images;
            imageListBox.DisplayMember = "ImageName";
            imageListBox.ValueMember = "Id";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading images: {ex.Message}");
        }
    }

    private async void ImageListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (imageListBox.SelectedItem is ImageInfo selectedImage)
        {
            await _apiClient.LoadImageToPictureBox(pictureBox1, selectedImage.Id);
        }
    }
}
```

## Getting Started

1. **Build and Run the API**:
   ```bash
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

2. **Test the API**:
   - Navigate to `https://localhost:7000` to see API information
   - Use Swagger UI at `https://localhost:7000/swagger` (in development)
   - Test endpoints with your WinForms application

3. **Database Setup**:
   - The API will create the database automatically
   - Sample data is seeded for testing
   - Update image paths in the database to point to actual image files

## Notes for .NET Framework 4.8 Compatibility

- Use `HttpClient` with `async/await` (available in .NET Framework 4.5+)
- Install `Newtonsoft.Json` NuGet package for JSON deserialization
- Ensure your WinForms app targets .NET Framework 4.8 or compatible
- Handle SSL certificate validation if using HTTPS in development

## Troubleshooting

- **CORS Issues**: API has CORS enabled for all origins
- **SSL Certificate**: Use `TrustServerCertificate=true` in development
- **File Not Found**: Ensure image paths in database point to existing files
- **Database Connection**: Verify connection string in appsettings.json