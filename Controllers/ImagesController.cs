using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ImageAPI.Data;
using ImageAPI.Models;

namespace ImageAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly ImageDbContext _context;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(ImageDbContext context, ILogger<ImagesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all image records with basic information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ImageInfoDto>>>> GetAllImages()
        {
            try
            {
                var images = await _context.Images
                    .Select(img => new ImageInfoDto
                    {
                        Id = img.Id,
                        ImageName = img.ImageName ?? "Unknown",
                        Description = img.Description,
                        CreatedDate = img.CreatedDate,
                        ModifiedDate = img.ModifiedDate,
                        ContentType = img.ContentType,
                        FileSize = img.FileSize,
                        FileExists = !string.IsNullOrEmpty(img.ImagePath) && System.IO.File.Exists(img.ImagePath),
                        DownloadUrl = $"/api/images/{img.Id}/download"
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<ImageInfoDto>>
                {
                    Success = true,
                    Message = $"Retrieved {images.Count} image records",
                    Data = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving image list");
                return StatusCode(500, new ApiResponse<List<ImageInfoDto>>
                {
                    Success = false,
                    Message = "Error retrieving image list"
                });
            }
        }

        /// <summary>
        /// Get a specific image record by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ImageInfoDto>>> GetImage(int id)
        {
            try
            {
                var image = await _context.Images.FindAsync(id);
                
                if (image == null)
                {
                    return NotFound(new ApiResponse<ImageInfoDto>
                    {
                        Success = false,
                        Message = $"Image with ID {id} not found"
                    });
                }

                var imageDto = new ImageInfoDto
                {
                    Id = image.Id,
                    ImageName = image.ImageName ?? "Unknown",
                    Description = image.Description,
                    CreatedDate = image.CreatedDate,
                    ModifiedDate = image.ModifiedDate,
                    ContentType = image.ContentType,
                    FileSize = image.FileSize,
                    FileExists = image.FileExists,
                    DownloadUrl = $"/api/images/{image.Id}/download"
                };

                return Ok(new ApiResponse<ImageInfoDto>
                {
                    Success = true,
                    Message = "Image record retrieved successfully",
                    Data = imageDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving image {ImageId}", id);
                return StatusCode(500, new ApiResponse<ImageInfoDto>
                {
                    Success = false,
                    Message = "Error retrieving image record"
                });
            }
        }

        /// <summary>
        /// Download the actual image file by ID
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadImage(int id)
        {
            try
            {
                var image = await _context.Images.FindAsync(id);
                
                if (image == null)
                {
                    return NotFound(new { message = $"Image with ID {id} not found" });
                }

                if (string.IsNullOrEmpty(image.ImagePath) || !System.IO.File.Exists(image.ImagePath))
                {
                    return NotFound(new { message = "Image file not found on disk" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImagePath);
                var contentType = image.ContentType ?? "application/octet-stream";
                var fileName = image.ImageName ?? $"image_{id}";

                // Ensure proper file extension
                if (!Path.HasExtension(fileName) && !string.IsNullOrEmpty(image.ContentType))
                {
                    var extension = image.ContentType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/gif" => ".gif",
                        "image/bmp" => ".bmp",
                        _ => ".bin"
                    };
                    fileName += extension;
                }

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading image {ImageId}", id);
                return StatusCode(500, new { message = "Error downloading image" });
            }
        }

        /// <summary>
        /// Get image as Base64 data URI (useful for WinForms)
        /// </summary>
        [HttpGet("{id}/base64")]
        public async Task<ActionResult<ApiResponse<string>>> GetImageBase64(int id)
        {
            try
            {
                var image = await _context.Images.FindAsync(id);
                
                if (image == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = $"Image with ID {id} not found"
                    });
                }

                if (string.IsNullOrEmpty(image.ImagePath) || !System.IO.File.Exists(image.ImagePath))
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Image file not found on disk"
                    });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImagePath);
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
                _logger.LogError(ex, "Error converting image {ImageId} to Base64", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Error converting image to Base64"
                });
            }
        }

        /// <summary>
        /// Get image as raw Base64 string (without data URI wrapper)
        /// </summary>
        [HttpGet("{id}/base64raw")]
        public async Task<ActionResult<ApiResponse<object>>> GetImageBase64Raw(int id)
        {
            try
            {
                var image = await _context.Images.FindAsync(id);
                
                if (image == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Image with ID {id} not found"
                    });
                }

                if (string.IsNullOrEmpty(image.ImagePath) || !System.IO.File.Exists(image.ImagePath))
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Image file not found on disk"
                    });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImagePath);
                var base64String = Convert.ToBase64String(fileBytes);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Image converted to Base64 successfully",
                    Data = new
                    {
                        Id = image.Id,
                        ImageName = image.ImageName,
                        ContentType = image.ContentType,
                        FileSize = fileBytes.Length,
                        Base64Data = base64String
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting image {ImageId} to raw Base64", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error converting image to Base64"
                });
            }
        }

        /// <summary>
        /// Get all images with their Base64 data (for complete data transfer)
        /// </summary>
        [HttpGet("all-base64")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetAllImagesWithBase64()
        {
            try
            {
                var images = await _context.Images.ToListAsync();
                var imageDataList = new List<object>();

                foreach (var image in images)
                {
                    var imageData = new
                    {
                        Id = image.Id,
                        ImageName = image.ImageName ?? "Unknown",
                        Description = image.Description,
                        CreatedDate = image.CreatedDate,
                        ModifiedDate = image.ModifiedDate,
                        ContentType = image.ContentType,
                        FileSize = image.FileSize,
                        FileExists = !string.IsNullOrEmpty(image.ImagePath) && System.IO.File.Exists(image.ImagePath),
                        Base64Data = (string?)null
                    };

                    // Only include Base64 data if file exists
                    if (imageData.FileExists)
                    {
                        try
                        {
                            var fileBytes = await System.IO.File.ReadAllBytesAsync(image.ImagePath);
                            var base64String = Convert.ToBase64String(fileBytes);
                            imageData = new
                            {
                                imageData.Id,
                                imageData.ImageName,
                                imageData.Description,
                                imageData.CreatedDate,
                                imageData.ModifiedDate,
                                imageData.ContentType,
                                FileSize = (long?)fileBytes.Length,
                                imageData.FileExists,
                                Base64Data = base64String
                            };
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not read image file for ID {ImageId}", image.Id);
                        }
                    }

                    imageDataList.Add(imageData);
                }

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = $"Retrieved {imageDataList.Count} images with Base64 data",
                    Data = imageDataList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all images with Base64 data");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Error retrieving images with Base64 data"
                });
            }
        }

        /// <summary>
        /// Search images by name or description
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<ImageInfoDto>>>> SearchImages([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new ApiResponse<List<ImageInfoDto>>
                    {
                        Success = false,
                        Message = "Search query cannot be empty"
                    });
                }

                var images = await _context.Images
                    .Where(img => img.ImageName!.Contains(query) || 
                                 (img.Description != null && img.Description.Contains(query)))
                    .Select(img => new ImageInfoDto
                    {
                        Id = img.Id,
                        ImageName = img.ImageName ?? "Unknown",
                        Description = img.Description,
                        CreatedDate = img.CreatedDate,
                        ModifiedDate = img.ModifiedDate,
                        ContentType = img.ContentType,
                        FileSize = img.FileSize,
                        FileExists = !string.IsNullOrEmpty(img.ImagePath) && System.IO.File.Exists(img.ImagePath),
                        DownloadUrl = $"/api/images/{img.Id}/download"
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<ImageInfoDto>>
                {
                    Success = true,
                    Message = $"Found {images.Count} images matching '{query}'",
                    Data = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching images with query: {Query}", query);
                return StatusCode(500, new ApiResponse<List<ImageInfoDto>>
                {
                    Success = false,
                    Message = "Error searching images"
                });
            }
        }
    }
}