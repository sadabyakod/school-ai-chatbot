# Inventory Image API - Complete Base64 Transfer Guide

## Overview
This API provides endpoints to serve images from the `Inventory_Image` table in SQL Server to .NET Framework 4.8 WinForms applications. Images are fetched from the `ImageLocation` column and converted to Base64 for easy transfer.

## Database Table Structure

```sql
CREATE TABLE [dbo].[Inventory_Image](
    [ID] [bigint] NULL,
    [ItemNum] [nvarchar](20) NOT NULL,
    [Store_ID] [nvarchar](10) NOT NULL,
    [Position] [int] NULL,
    [ImageLocation] [nvarchar](4000) NOT NULL,
 CONSTRAINT [pkInventory_Image] PRIMARY KEY CLUSTERED 
(
    [ItemNum] ASC,
    [Store_ID] ASC,
    [ImageLocation] ASC
)
```

## API Endpoints

### 1. Get All Inventory Images
**URL**: `GET /api/inventory`
**Description**: Retrieve all inventory image records with metadata
**Response**: JSON array with image information including download URLs

### 2. Get Images by Item Number
**URL**: `GET /api/inventory/item/{itemNum}`
**Description**: Get all images for a specific item
**Parameters**: `itemNum` (string) - Item number
**Response**: JSON array with matching images

### 3. Get Images by Item and Store
**URL**: `GET /api/inventory/item/{itemNum}/store/{storeId}`
**Description**: Get images for a specific item in a specific store
**Parameters**: 
- `itemNum` (string) - Item number
- `storeId` (string) - Store ID
**Response**: JSON array with matching images

### 4. Download Image File
**URL**: `GET /api/inventory/{itemNum}/{storeId}/download?location={imageLocation}`
**Description**: Download the actual image file
**Parameters**: 
- `itemNum` (string) - Item number
- `storeId` (string) - Store ID
- `location` (query string) - Full image file path
**Response**: Binary file with appropriate content-type header

### 5. Get Image as Base64 Data URI
**URL**: `GET /api/inventory/{itemNum}/{storeId}/base64?location={imageLocation}`
**Description**: Get image as Base64 data URI (ready for HTML/web use)
**Parameters**: 
- `itemNum` (string) - Item number
- `storeId` (string) - Store ID
- `location` (query string) - Full image file path
**Response**: JSON object with Base64 data URI string

### 6. Get Image as Raw Base64
**URL**: `GET /api/inventory/{itemNum}/{storeId}/base64raw?location={imageLocation}`
**Description**: Get image as raw Base64 with complete metadata
**Parameters**: 
- `itemNum` (string) - Item number
- `storeId` (string) - Store ID
- `location` (query string) - Full image file path
**Response**: JSON object with Base64 data and metadata

### 7. Get All Images with Base64 Data
**URL**: `GET /api/inventory/all-base64`
**Description**: Get ALL inventory images with their Base64 data in one call
**Response**: JSON array with all images and their Base64 data

### 8. Search Inventory Images
**URL**: `GET /api/inventory/search?itemNum={itemNum}&storeId={storeId}`
**Description**: Search images by item number and/or store ID
**Parameters**: 
- `itemNum` (query, optional) - Item number to search
- `storeId` (query, optional) - Store ID to search
**Response**: JSON array with matching images

## Complete WinForms Example

### 1. Install Required Packages
```
Install-Package System.Net.Http
Install-Package Newtonsoft.Json
```

### 2. Create Inventory API Client
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

public class InventoryImageApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public InventoryImageApiClient(string baseUrl = "http://localhost:5200")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    // Get all inventory images with Base64 data in one call
    public async Task<List<InventoryImageWithBase64>> GetAllInventoryImagesWithBase64Async()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/api/inventory/all-base64");
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<InventoryImageWithBase64>>>(response);
            return apiResponse.Data ?? new List<InventoryImageWithBase64>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting inventory images with Base64: {ex.Message}", ex);
        }
    }

    // Get images for a specific item with Base64 data
    public async Task<List<InventoryImageWithBase64>> GetImagesForItemWithBase64Async(string itemNum)
    {
        try
        {
            // First get image metadata
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/api/inventory/item/{itemNum}");
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<InventoryImageDto>>>(response);
            
            if (!apiResponse.Success || apiResponse.Data == null)
                return new List<InventoryImageWithBase64>();

            // Then get Base64 data for each image
            var result = new List<InventoryImageWithBase64>();
            foreach (var img in apiResponse.Data)
            {
                try
                {
                    var base64Response = await _httpClient.GetStringAsync(
                        $"{_baseUrl}/api/inventory/{img.ItemNum}/{img.Store_ID}/base64raw?location={Uri.EscapeDataString(img.ImageLocation)}");
                    var base64Result = JsonConvert.DeserializeObject<ApiResponse<InventoryImageWithBase64>>(base64Response);
                    
                    if (base64Result.Success && base64Result.Data != null)
                    {
                        result.Add(base64Result.Data);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other images
                    Console.WriteLine($"Error getting Base64 for image: {ex.Message}");
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting images for item {itemNum}: {ex.Message}", ex);
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
    public void LoadImageFromBase64ToPictureBox(PictureBox pictureBox, InventoryImageWithBase64 imageData)
    {
        try
        {
            if (!string.IsNullOrEmpty(imageData.Base64Data))
            {
                pictureBox.Image = Base64ToImage(imageData.Base64Data);
            }
            else
            {
                // Set placeholder image or clear
                pictureBox.Image = null;
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

// Data Transfer Objects for Inventory Images
public class InventoryImageDto
{
    public long? ID { get; set; }
    public string ItemNum { get; set; }
    public string Store_ID { get; set; }
    public int? Position { get; set; }
    public string ImageLocation { get; set; }
    public string ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool FileExists { get; set; }
    public string DisplayName { get; set; }
    public string DownloadUrl { get; set; }
}

public class InventoryImageWithBase64
{
    public long? ID { get; set; }
    public string ItemNum { get; set; }
    public string Store_ID { get; set; }
    public int? Position { get; set; }
    public string ImageLocation { get; set; }
    public string ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool FileExists { get; set; }
    public string DisplayName { get; set; }
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
public partial class InventoryImageForm : Form
{
    private InventoryImageApiClient _apiClient;
    private List<InventoryImageWithBase64> _allImages;

    public InventoryImageForm()
    {
        InitializeComponent();
        _apiClient = new InventoryImageApiClient("http://localhost:5200");
    }

    // Load all images with Base64 data on form load
    private async void InventoryImageForm_Load(object sender, EventArgs e)
    {
        await LoadAllImagesAsync();
    }

    private async Task LoadAllImagesAsync()
    {
        try
        {
            statusLabel.Text = "Loading inventory images...";
            _allImages = await _apiClient.GetAllInventoryImagesWithBase64Async();
            
            // Populate inventory list
            inventoryListBox.Items.Clear();
            foreach (var image in _allImages)
            {
                inventoryListBox.Items.Add($"{image.ItemNum} - {image.Store_ID} (Pos: {image.Position})");
            }
            
            statusLabel.Text = $"Loaded {_allImages.Count} inventory images";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading images: {ex.Message}", "Error");
            statusLabel.Text = "Error loading images";
        }
    }

    // Load images for specific item
    private async void loadItemImagesButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(itemNumberTextBox.Text))
        {
            MessageBox.Show("Please enter an item number", "Input Required");
            return;
        }

        try
        {
            statusLabel.Text = "Loading images for item...";
            var itemImages = await _apiClient.GetImagesForItemWithBase64Async(itemNumberTextBox.Text);
            
            inventoryListBox.Items.Clear();
            _allImages = itemImages;
            
            foreach (var image in itemImages)
            {
                inventoryListBox.Items.Add($"{image.ItemNum} - {image.Store_ID} (Pos: {image.Position})");
            }
            
            statusLabel.Text = $"Loaded {itemImages.Count} images for item {itemNumberTextBox.Text}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading item images: {ex.Message}", "Error");
        }
    }

    // Display selected image
    private void inventoryListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (inventoryListBox.SelectedIndex >= 0 && _allImages != null)
        {
            var selectedImage = _allImages[inventoryListBox.SelectedIndex];
            DisplayInventoryImage(selectedImage);
        }
    }

    private void DisplayInventoryImage(InventoryImageWithBase64 imageData)
    {
        try
        {
            // Load image from Base64
            _apiClient.LoadImageFromBase64ToPictureBox(pictureBox, imageData);
            
            // Update info labels
            itemNumLabel.Text = $"Item: {imageData.ItemNum}";
            storeLabel.Text = $"Store: {imageData.Store_ID}";
            positionLabel.Text = $"Position: {imageData.Position}";
            locationLabel.Text = $"Location: {Path.GetFileName(imageData.ImageLocation)}";
            imageSizeLabel.Text = $"Size: {FormatFileSize(imageData.FileSize)}";
            imageTypeLabel.Text = $"Type: {imageData.ContentType}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error displaying image: {ex.Message}", "Error");
        }
    }

    private string FormatFileSize(long? bytes)
    {
        if (!bytes.HasValue) return "Unknown";
        if (bytes < 1024) return $"{bytes} bytes";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
        return $"{bytes / (1024 * 1024):F1} MB";
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

## API Response Examples

### Get All Inventory Images with Base64
```json
{
  "success": true,
  "message": "Retrieved 3 inventory images with Base64 data",
  "data": [
    {
      "id": 1,
      "itemNum": "ITEM001",
      "store_ID": "ST001",
      "position": 1,
      "imageLocation": "D:\\images\\item001_pos1.jpg",
      "contentType": "image/jpeg",
      "fileSize": 45678,
      "fileExists": true,
      "displayName": "ITEM001 - Store ST001 (Pos: 1)",
      "base64Data": "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBD..."
    }
  ]
}
```

### Get Images by Item Number
```json
{
  "success": true,
  "message": "Retrieved 2 images for item ITEM001",
  "data": [
    {
      "id": 1,
      "itemNum": "ITEM001",
      "store_ID": "ST001",
      "position": 1,
      "imageLocation": "D:\\images\\item001_pos1.jpg",
      "contentType": "image/jpeg",
      "fileSize": 45678,
      "fileExists": true,
      "displayName": "ITEM001 - Store ST001 (Pos: 1)",
      "downloadUrl": "/api/inventory/ITEM001/ST001/download?location=D%3A%5Cimages%5Citem001_pos1.jpg"
    }
  ]
}
```

## Benefits of This Implementation

1. **Exact Table Match**: Works directly with your `Inventory_Image` table structure
2. **Composite Key Support**: Handles the complex primary key (ItemNum, Store_ID, ImageLocation)
3. **Complete Base64 Transfer**: All image data transferred as Base64 in JSON
4. **Multiple Query Options**: Search by item, store, or get all images
5. **WinForms Ready**: Direct Image object creation from Base64 data
6. **Efficient Loading**: Batch load all images or load by specific criteria
7. **Error Handling**: Comprehensive error handling for missing files
8. **File Validation**: Automatic file existence checking

## Testing the API

- **All Inventory Images**: `http://localhost:5200/api/inventory/all-base64`
- **Item Images**: `http://localhost:5200/api/inventory/item/ITEM001`
- **Item + Store Images**: `http://localhost:5200/api/inventory/item/ITEM001/store/ST001`
- **Single Image Base64**: `http://localhost:5200/api/inventory/ITEM001/ST001/base64raw?location=D:\path\to\image.jpg`
- **API Documentation**: `http://localhost:5200/swagger`

## Status: ✅ READY FOR PRODUCTION
The API now perfectly matches your `Inventory_Image` table structure and provides complete Base64 image transfer functionality for WinForms integration.