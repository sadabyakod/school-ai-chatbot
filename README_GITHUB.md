# Image API - .NET Core Web API for WinForms Integration

A professional .NET 9.0 Web API designed to serve images from SQL Server to .NET Framework 4.8 WinForms applications with Base64 encoding support.

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- SQL Server or LocalDB
- Visual Studio 2022 or VS Code

### Running the API

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd <repository-name>
   ```

2. **Configure the database**
   ```bash
   # Update connection string in appsettings.json
   # Run migrations (if using Entity Framework)
   dotnet ef database update
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the API**
   - Swagger UI: `https://localhost:7200/swagger`
   - API Base URL: `https://localhost:7200`

## 📋 API Endpoints

### Images Controller

#### Get Image by Item Number
```http
GET /api/images/{itemNum}
```
Returns image metadata and Base64-encoded image data.

**Response:**
```json
{
  "itemNum": "ITEM001",
  "description": "Sample Item",
  "imageBase64": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "mimeType": "image/png",
  "fileSize": 12345,
  "lastModified": "2024-01-01T00:00:00Z"
}
```

#### Search Images
```http
GET /api/images/search?query=searchterm
```

#### Get All Images
```http
GET /api/images
```

## 🏗️ Architecture

### Key Components

- **Controllers**: RESTful API endpoints
- **Data**: Entity Framework context and models
- **Models**: DTOs and data entities
- **Documentation**: API examples and integration guides

### Database Schema

```sql
CREATE TABLE Images (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ItemNum nvarchar(50) NOT NULL UNIQUE,
    Description nvarchar(500),
    ImageData varbinary(max),
    MimeType nvarchar(100),
    FileSize bigint,
    CreatedDate datetime2 DEFAULT GETDATE(),
    LastModified datetime2 DEFAULT GETDATE()
);
```

## 🔧 Configuration

### Connection String
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ImageApiDb;Trusted_Connection=true;"
  }
}
```

### CORS Configuration
The API is pre-configured with CORS support for cross-origin requests.

## 📱 WinForms Integration

### Sample C# Code for .NET Framework 4.8

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ImageApiClient
{
    private readonly HttpClient httpClient;
    private readonly string baseUrl;

    public ImageApiClient(string apiBaseUrl)
    {
        baseUrl = apiBaseUrl;
        httpClient = new HttpClient();
    }

    public async Task<ImageResponse> GetImageAsync(string itemNum)
    {
        try
        {
            var response = await httpClient.GetAsync($"{baseUrl}/api/images/{itemNum}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ImageResponse>(json);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching image: {ex.Message}");
        }
    }
}

public class ImageResponse
{
    public string ItemNum { get; set; }
    public string Description { get; set; }
    public string ImageBase64 { get; set; }
    public string MimeType { get; set; }
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
}
```

### Displaying Images in WinForms

```csharp
private async void LoadImage(string itemNum)
{
    try
    {
        var imageResponse = await apiClient.GetImageAsync(itemNum);
        
        if (imageResponse != null && !string.IsNullOrEmpty(imageResponse.ImageBase64))
        {
            // Remove data URL prefix if present
            var base64Data = imageResponse.ImageBase64;
            if (base64Data.StartsWith("data:image"))
            {
                base64Data = base64Data.Substring(base64Data.IndexOf(',') + 1);
            }
            
            // Convert to image
            var imageBytes = Convert.FromBase64String(base64Data);
            using (var ms = new MemoryStream(imageBytes))
            {
                pictureBox.Image = Image.FromStream(ms);
            }
            
            descriptionLabel.Text = imageResponse.Description;
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading image: {ex.Message}");
    }
}
```

## 🛠️ Development

### Project Structure
```
├── Controllers/          # API controllers
├── Data/                # Entity Framework context
├── Models/              # Data models and DTOs
├── Documentation/       # API documentation and examples
├── TestImages/          # Sample images for testing
├── Program.cs           # Application startup
└── README.md           # This file
```

### Building the Project
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Run tests (if available)
dotnet test
```

## 📦 Deployment

### Local Development
- Uses in-memory database for development
- Swagger UI enabled
- CORS configured for local testing

### Production
- Configure SQL Server connection string
- Update CORS policy for production domains
- Set production environment variables

## 🔒 Security Considerations

- Input validation on all endpoints
- SQL injection protection via Entity Framework
- File size limits for image uploads
- CORS policy configuration

## 📚 API Documentation

- **Swagger UI**: Available at `/swagger` in development
- **OpenAPI Spec**: Auto-generated from controllers
- **Examples**: See `Documentation/` folder

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For issues and questions:
1. Check the documentation in the `Documentation/` folder
2. Review the API examples
3. Create an issue in the repository

## 🚀 Features

- ✅ RESTful API design
- ✅ Base64 image encoding
- ✅ SQL Server integration
- ✅ Entity Framework Core
- ✅ Swagger documentation
- ✅ CORS support
- ✅ .NET Framework 4.8 compatibility
- ✅ Professional error handling
- ✅ Async/await patterns
- ✅ Comprehensive logging

## 📈 Status

**Current Version**: 1.0.0  
**Status**: ✅ Production Ready  
**Last Updated**: November 2024