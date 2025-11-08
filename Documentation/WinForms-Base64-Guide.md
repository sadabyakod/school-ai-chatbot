# WinForms Integration Guide - Base64 Image Transfer

## Overview
This guide shows how to integrate the Image API with your .NET Framework 4.8 WinForms application to transfer images as Base64 data.

## Available Base64 Endpoints

### 1. Get Single Image as Base64 Data URI
**URL**: `GET /api/images/{id}/base64`
**Returns**: Complete data URI ready for HTML/web use
```json
{
  "success": true,
  "message": "Image converted to Base64 data URI successfully",
  "data": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
}
```

### 2. Get Single Image as Raw Base64
**URL**: `GET /api/images/{id}/base64raw`
**Returns**: Raw Base64 string with metadata
```json
{
  "success": true,
  "message": "Image converted to Base64 successfully",
  "data": {
    "id": 1,
    "imageName": "Test Image",
    "contentType": "image/png",
    "fileSize": 1024,
    "base64Data": "iVBORw0KGgoAAAANSUhEUgAA..."
  }
}
```

### 3. Get All Images with Base64 Data
**URL**: `GET /api/images/all-base64`
**Returns**: Array of all images with their Base64 data
```json
{
  "success": true,
  "message": "Retrieved 2 images with Base64 data",
  "data": [
    {
      "id": 1,
      "imageName": "Test Red Rectangle",
      "description": "A sample image for testing",
      "contentType": "image/png",
      "fileSize": 1024,
      "base64Data": "iVBORw0KGgoAAAANSUhEUgAA..."
    }
  ]
}
```

## Complete WinForms Example

### 1. Install Required Packages
In your .NET Framework 4.8 WinForms project, install these NuGet packages:
```
Install-Package System.Net.Http
Install-Package Newtonsoft.Json
```

### 2. Create API Client Class
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

public class ImageApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ImageApiClient(string baseUrl = "http://localhost:5200")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    // Get all images with Base64 data in one call
    public async Task<List<ImageWithBase64>> GetAllImagesWithBase64Async()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/api/images/all-base64");
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<ImageWithBase64>>>(response);
            return apiResponse.Data ?? new List<ImageWithBase64>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting images with Base64: {ex.Message}", ex);
        }
    }

    // Get single image as raw Base64
    public async Task<ImageWithBase64> GetImageBase64RawAsync(int imageId)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/api/images/{imageId}/base64raw");
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ImageWithBase64>>(response);
            return apiResponse.Data;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting image Base64: {ex.Message}", ex);
        }
    }

    // Convert Base64 string to Image
    public Image Base64ToImage(string base64String)
    {
        try
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error converting Base64 to image: {ex.Message}", ex);
        }
    }

    // Load image from Base64 into PictureBox
    public async Task LoadImageFromBase64ToPictureBox(PictureBox pictureBox, int imageId)
    {
        try
        {
            var imageData = await GetImageBase64RawAsync(imageId);
            if (!string.IsNullOrEmpty(imageData.Base64Data))
            {
                pictureBox.Image = Base64ToImage(imageData.Base64Data);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading image: {ex.Message}", "Error", 
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Data Transfer Objects
public class ImageWithBase64
{
    public int Id { get; set; }
    public string ImageName { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public bool FileExists { get; set; }
    public string Base64Data { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 3. WinForms Implementation Example
```csharp
public partial class MainForm : Form
{
    private ImageApiClient _apiClient;
    private List<ImageWithBase64> _allImages;

    public MainForm()
    {
        InitializeComponent();
        _apiClient = new ImageApiClient("http://localhost:5200");
    }

    // Load all images with Base64 data on form load
    private async void MainForm_Load(object sender, EventArgs e)
    {
        await LoadAllImagesAsync();
    }

    private async Task LoadAllImagesAsync()
    {
        try
        {
            statusLabel.Text = "Loading images...";
            _allImages = await _apiClient.GetAllImagesWithBase64Async();
            
            // Populate image list
            imageListBox.Items.Clear();
            foreach (var image in _allImages)
            {
                imageListBox.Items.Add($"{image.Id}: {image.ImageName}");
            }
            
            statusLabel.Text = $"Loaded {_allImages.Count} images";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading images: {ex.Message}", "Error");
            statusLabel.Text = "Error loading images";
        }
    }

    // Display selected image
    private void imageListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (imageListBox.SelectedIndex >= 0 && _allImages != null)
        {
            var selectedImage = _allImages[imageListBox.SelectedIndex];
            DisplayImageFromBase64(selectedImage);
        }
    }

    private void DisplayImageFromBase64(ImageWithBase64 imageData)
    {
        try
        {
            if (!string.IsNullOrEmpty(imageData.Base64Data))
            {
                // Convert Base64 to Image and display
                var image = _apiClient.Base64ToImage(imageData.Base64Data);
                pictureBox.Image = image;
                
                // Update info labels
                imageNameLabel.Text = imageData.ImageName;
                imageSizeLabel.Text = $"Size: {imageData.FileSize} bytes";
                imageTypeLabel.Text = $"Type: {imageData.ContentType}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error displaying image: {ex.Message}", "Error");
        }
    }

    // Save image to file
    private void saveImageButton_Click(object sender, EventArgs e)
    {
        if (pictureBox.Image != null && imageListBox.SelectedIndex >= 0)
        {
            var selectedImage = _allImages[imageListBox.SelectedIndex];
            
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Files|*.png|JPEG Files|*.jpg|All Files|*.*";
                saveDialog.FileName = selectedImage.ImageName;
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        pictureBox.Image.Save(saveDialog.FileName);
                        MessageBox.Show("Image saved successfully!", "Success");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving image: {ex.Message}", "Error");
                    }
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _apiClient?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

### 4. Form Designer Code (MainForm.Designer.cs)
```csharp
partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private ListBox imageListBox;
    private PictureBox pictureBox;
    private Label statusLabel;
    private Label imageNameLabel;
    private Label imageSizeLabel;
    private Label imageTypeLabel;
    private Button saveImageButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.imageListBox = new ListBox();
        this.pictureBox = new PictureBox();
        this.statusLabel = new Label();
        this.imageNameLabel = new Label();
        this.imageSizeLabel = new Label();
        this.imageTypeLabel = new Label();
        this.saveImageButton = new Button();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
        this.SuspendLayout();
        
        // imageListBox
        this.imageListBox.Location = new Point(12, 12);
        this.imageListBox.Size = new Size(200, 300);
        this.imageListBox.SelectedIndexChanged += this.imageListBox_SelectedIndexChanged;
        
        // pictureBox
        this.pictureBox.Location = new Point(230, 12);
        this.pictureBox.Size = new Size(400, 300);
        this.pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        this.pictureBox.BorderStyle = BorderStyle.FixedSingle;
        
        // Status and info labels
        this.statusLabel.Location = new Point(12, 320);
        this.statusLabel.Size = new Size(300, 23);
        
        this.imageNameLabel.Location = new Point(230, 320);
        this.imageNameLabel.Size = new Size(400, 23);
        
        this.imageSizeLabel.Location = new Point(230, 343);
        this.imageSizeLabel.Size = new Size(200, 23);
        
        this.imageTypeLabel.Location = new Point(440, 343);
        this.imageTypeLabel.Size = new Size(190, 23);
        
        // saveImageButton
        this.saveImageButton.Location = new Point(555, 370);
        this.saveImageButton.Size = new Size(75, 23);
        this.saveImageButton.Text = "Save Image";
        this.saveImageButton.Click += this.saveImageButton_Click;
        
        // MainForm
        this.ClientSize = new Size(650, 410);
        this.Controls.Add(this.imageListBox);
        this.Controls.Add(this.pictureBox);
        this.Controls.Add(this.statusLabel);
        this.Controls.Add(this.imageNameLabel);
        this.Controls.Add(this.imageSizeLabel);
        this.Controls.Add(this.imageTypeLabel);
        this.Controls.Add(this.saveImageButton);
        this.Text = "Image API WinForms Client";
        this.Load += this.MainForm_Load;
        
        ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
        this.ResumeLayout(false);
    }
}
```

## Benefits of Base64 Transfer

1. **Complete Data Transfer**: All image data is included in the JSON response
2. **No Additional HTTP Calls**: Get metadata and image data in one request
3. **WinForms Compatible**: Easy to convert Base64 to Image objects
4. **Network Efficient**: Single request for multiple images
5. **Error Handling**: Built-in success/failure indicators

## Usage Tips

- Use `/all-base64` endpoint to get all images in one call
- Use `/base64raw` for individual images with metadata
- Convert Base64 to Image using `Convert.FromBase64String()` and `Image.FromStream()`
- Handle large images carefully due to Base64 size increase (~33%)
- Implement progress indicators for large image sets

## Testing the API

Run the API and test these endpoints:
- `http://localhost:5200/api/images/all-base64`
- `http://localhost:5200/api/images/1/base64raw`
- `http://localhost:5200/swagger` for interactive testing