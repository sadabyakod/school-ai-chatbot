using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ImageAPI.Data;
using ImageAPI.Models;

namespace ImageAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly ImageDbContext _context;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(ImageDbContext context, ILogger<InventoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all inventory images with basic information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<InventoryImageDto>>>> GetAllInventoryImages()
        {
            try
            {
                var images = await _context.InventoryImages
                    .Select(img => new InventoryImageDto
                    {
                        ID = img.ID,
                        ItemNum = img.ItemNum,
                        Store_ID = img.Store_ID,
                        Position = img.Position,
                        ImageLocation = img.ImageLocation,
                        ContentType = img.ContentType,
                        FileSize = img.FileSize,
                        FileExists = !string.IsNullOrEmpty(img.ImageLocation) && System.IO.File.Exists(img.ImageLocation),
                        DisplayName = img.DisplayName,
                        DownloadUrl = $"/api/inventory/{Uri.EscapeDataString(img.ItemNum)}/{Uri.EscapeDataString(img.Store_ID)}/download?location={Uri.EscapeDataString(img.ImageLocation)}"
                    })
                    .OrderBy(x => x.ItemNum)
                    .ThenBy(x => x.Store_ID)
                    .ThenBy(x => x.Position)
                    .ToListAsync();

                return Ok(new ApiResponse<List<InventoryImageDto>>
                {
                    Success = true,
                    Message = $"Retrieved {images.Count} inventory image records",
                    Data = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inventory image list");
                return StatusCode(500, new ApiResponse<List<InventoryImageDto>>
                {
                    Success = false,
                    Message = "Error retrieving inventory image list"
                });
            }
        }

        /// <summary>
        /// Get inventory images by Item Number
        /// </summary>
        [HttpGet("item/{itemNum}")]
        public async Task<ActionResult<ApiResponse<List<InventoryImageDto>>>> GetImagesByItem(string itemNum)
        {
            try
            {
                var images = await _context.InventoryImages
                    .Where(img => img.ItemNum == itemNum)
                    .Select(img => new InventoryImageDto
                    {
                        ID = img.ID,
                        ItemNum = img.ItemNum,
                        Store_ID = img.Store_ID,
                        Position = img.Position,
                        ImageLocation = img.ImageLocation,
                        ContentType = img.ContentType,
                        FileSize = img.FileSize,
                        FileExists = !string.IsNullOrEmpty(img.ImageLocation) && System.IO.File.Exists(img.ImageLocation),
                        DisplayName = img.DisplayName,
                        DownloadUrl = $"/api/inventory/{Uri.EscapeDataString(img.ItemNum)}/{Uri.EscapeDataString(img.Store_ID)}/download?location={Uri.EscapeDataString(img.ImageLocation)}"
                    })
                    .OrderBy(x => x.Store_ID)
                    .ThenBy(x => x.Position)
                    .ToListAsync();

                return Ok(new ApiResponse<List<InventoryImageDto>>
                {
                    Success = true,
                    Message = $"Retrieved {images.Count} images for item {itemNum}",
                    Data = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images for item {ItemNum}", itemNum);
                return StatusCode(500, new ApiResponse<List<InventoryImageDto>>
                {
                    Success = false,
                    Message = "Error retrieving images for item"
                });
            }
        }

        /// <summary>
        /// Get inventory images by Item Number and Store ID
        /// </summary>
        [HttpGet("item/{itemNum}/store/{storeId}")]
        public async Task<ActionResult<ApiResponse<List<InventoryImageDto>>>> GetImagesByItemAndStore(string itemNum, string storeId)
        {
            try
            {
                var images = await _context.InventoryImages
                    .Where(img => img.ItemNum == itemNum && img.Store_ID == storeId)
                    .Select(img => new InventoryImageDto
                    {
                        ID = img.ID,
                        ItemNum = img.ItemNum,
                        Store_ID = img.Store_ID,
                        Position = img.Position,
                        ImageLocation = img.ImageLocation,
                        ContentType = img.ContentType,
                        FileSize = img.FileSize,
                        FileExists = !string.IsNullOrEmpty(img.ImageLocation) && System.IO.File.Exists(img.ImageLocation),
                        DisplayName = img.DisplayName,
                        DownloadUrl = $"/api/inventory/{Uri.EscapeDataString(img.ItemNum)}/{Uri.EscapeDataString(img.Store_ID)}/download?location={Uri.EscapeDataString(img.ImageLocation)}"
                    })
                    .OrderBy(x => x.Position)
                    .ToListAsync();

                return Ok(new ApiResponse<List<InventoryImageDto>>
                {
                    Success = true,
                    Message = $"Retrieved {images.Count} images for item {itemNum} in store {storeId}",
                    Data = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images for item {ItemNum} and store {StoreId}", itemNum, storeId);
                return StatusCode(500, new ApiResponse<List<InventoryImageDto>>
                {
                    Success = false,
                    Message = "Error retrieving images for item and store"
                });
            }
        }

        /// <summary>
        /// Download image file by ItemNum, StoreId, and ImageLocation
        /// </summary>
        [HttpGet("{itemNum}/{storeId}/download")]
        public async Task<IActionResult> DownloadInventoryImage(string itemNum, string storeId, [FromQuery] string location)
        {
            try
            {
                var image = await _context.InventoryImages
                    .FirstOrDefaultAsync(img => img.ItemNum == itemNum && 
                                               img.Store_ID == storeId && 
                                               img.ImageLocation == location);
                
                if (image == null)
                {
                    return NotFound(new { message = $"Image not found for item {itemNum} in store {storeId}" });
                }

                if (string.IsNullOrEmpty(image.ImageLocation) || !System.IO.File.Exists(image.ImageLocation))
                {
                    return NotFound(new { message = "Image file not found on disk" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImageLocation);
                var contentType = image.ContentType ?? "application/octet-stream";
                var fileName = Path.GetFileName(image.ImageLocation) ?? $"{itemNum}_{storeId}_{image.Position}";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading image for item {ItemNum} store {StoreId}", itemNum, storeId);
                return StatusCode(500, new { message = "Error downloading image" });
            }
        }

        /// <summary>
        /// Get inventory image as Base64 data URI
        /// </summary>
        [HttpGet("{itemNum}/{storeId}/base64")]
        public async Task<ActionResult<ApiResponse<string>>> GetInventoryImageBase64(string itemNum, string storeId, [FromQuery] string location)
        {
            try
            {
                var image = await _context.InventoryImages
                    .FirstOrDefaultAsync(img => img.ItemNum == itemNum && 
                                               img.Store_ID == storeId && 
                                               img.ImageLocation == location);
                
                if (image == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = $"Image not found for item {itemNum} in store {storeId}"
                    });
                }

                if (string.IsNullOrEmpty(image.ImageLocation) || !System.IO.File.Exists(image.ImageLocation))
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Image file not found on disk"
                    });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImageLocation);
                var base64String = Convert.ToBase64String(fileBytes);
                var dataUri = $"data:{image.ContentType ?? "image/jpeg"};base64,{base64String}";

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Image converted to Base64 data URI successfully",
                    Data = dataUri
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting image to Base64 for item {ItemNum} store {StoreId}", itemNum, storeId);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error converting image to Base64"
                });
            }
        }

        /// <summary>
        /// Get inventory image as raw Base64 with metadata
        /// </summary>
        [HttpGet("{itemNum}/{storeId}/base64raw")]
        public async Task<ActionResult<ApiResponse<InventoryImageWithBase64>>> GetInventoryImageBase64Raw(string itemNum, string storeId, [FromQuery] string location)
        {
            try
            {
                var image = await _context.InventoryImages
                    .FirstOrDefaultAsync(img => img.ItemNum == itemNum && 
                                               img.Store_ID == storeId && 
                                               img.ImageLocation == location);
                
                if (image == null)
                {
                    return NotFound(new ApiResponse<InventoryImageWithBase64>
                    {
                        Success = false,
                        Message = $"Image not found for item {itemNum} in store {storeId}"
                    });
                }

                if (string.IsNullOrEmpty(image.ImageLocation) || !System.IO.File.Exists(image.ImageLocation))
                {
                    return NotFound(new ApiResponse<InventoryImageWithBase64>
                    {
                        Success = false,
                        Message = "Image file not found on disk"
                    });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImageLocation);
                var base64String = Convert.ToBase64String(fileBytes);

                var result = new InventoryImageWithBase64
                {
                    ID = image.ID,
                    ItemNum = image.ItemNum,
                    Store_ID = image.Store_ID,
                    Position = image.Position,
                    ImageLocation = image.ImageLocation,
                    ContentType = image.ContentType,
                    FileSize = fileBytes.Length,
                    FileExists = true,
                    DisplayName = image.DisplayName,
                    Base64Data = base64String
                };

                return Ok(new ApiResponse<InventoryImageWithBase64>
                {
                    Success = true,
                    Message = "Image converted to Base64 successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting image to raw Base64 for item {ItemNum} store {StoreId}", itemNum, storeId);
                return StatusCode(500, new ApiResponse<InventoryImageWithBase64>
                {
                    Success = false,
                    Message = "Error converting image to Base64"
                });
            }
        }

        /// <summary>
        /// Get all inventory images with their Base64 data (for complete data transfer)
        /// </summary>
        [HttpGet("all-base64")]
        public async Task<ActionResult<ApiResponse<List<InventoryImageWithBase64>>>> GetAllInventoryImagesWithBase64()
        {
            try
            {
                var images = await _context.InventoryImages
                    .OrderBy(x => x.ItemNum)
                    .ThenBy(x => x.Store_ID)
                    .ThenBy(x => x.Position)
                    .ToListAsync();

                var imageDataList = new List<InventoryImageWithBase64>();

                foreach (var image in images)
                {
                    var imageData = new InventoryImageWithBase64
                    {
                        ID = image.ID,
                        ItemNum = image.ItemNum,
                        Store_ID = image.Store_ID,
                        Position = image.Position,
                        ImageLocation = image.ImageLocation,
                        ContentType = image.ContentType,
                        FileSize = image.FileSize,
                        FileExists = image.FileExists,
                        DisplayName = image.DisplayName,
                        Base64Data = null
                    };

                    // Only include Base64 data if file exists
                    if (image.FileExists)
                    {
                        try
                        {
                            var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImageLocation);
                            var base64String = Convert.ToBase64String(fileBytes);
                            imageData.Base64Data = base64String;
                            imageData.FileSize = fileBytes.Length;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not read image file for item {ItemNum} store {StoreId}", image.ItemNum, image.Store_ID);
                        }
                    }

                    imageDataList.Add(imageData);
                }

                return Ok(new ApiResponse<List<InventoryImageWithBase64>>
                {
                    Success = true,
                    Message = $"Retrieved {imageDataList.Count} inventory images with Base64 data",
                    Data = imageDataList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all inventory images with Base64 data");
                return StatusCode(500, new ApiResponse<List<InventoryImageWithBase64>>
                {
                    Success = false,
                    Message = "Error retrieving inventory images with Base64 data"
                });
            }
        }

        /// <summary>
        /// Search inventory images by item number
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<InventoryImageDto>>>> SearchInventoryImages([FromQuery] string? itemNum, [FromQuery] string? storeId)
        {
            try
            {
                var query = _context.InventoryImages.AsQueryable();

                if (!string.IsNullOrWhiteSpace(itemNum))
                {
                    query = query.Where(img => img.ItemNum.Contains(itemNum));
                }

                if (!string.IsNullOrWhiteSpace(storeId))
                {
                    query = query.Where(img => img.Store_ID.Contains(storeId));
                }

                var images = await query
                    .Select(img => new InventoryImageDto
                    {
                        ID = img.ID,
                        ItemNum = img.ItemNum,
                        Store_ID = img.Store_ID,
                        Position = img.Position,
                        ImageLocation = img.ImageLocation,
                        ContentType = img.ContentType,
                        FileSize = img.FileSize,
                        FileExists = !string.IsNullOrEmpty(img.ImageLocation) && System.IO.File.Exists(img.ImageLocation),
                        DisplayName = img.DisplayName,
                        DownloadUrl = $"/api/inventory/{Uri.EscapeDataString(img.ItemNum)}/{Uri.EscapeDataString(img.Store_ID)}/download?location={Uri.EscapeDataString(img.ImageLocation)}"
                    })
                    .OrderBy(x => x.ItemNum)
                    .ThenBy(x => x.Store_ID)
                    .ThenBy(x => x.Position)
                    .ToListAsync();

                return Ok(new ApiResponse<List<InventoryImageDto>>
                {
                    Success = true,
                    Message = $"Found {images.Count} inventory images matching search criteria",
                    Data = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching inventory images");
                return StatusCode(500, new ApiResponse<List<InventoryImageDto>>
                {
                    Success = false,
                    Message = "Error searching inventory images"
                });
            }
        }
    }
}