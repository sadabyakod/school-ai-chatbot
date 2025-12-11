using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SchoolAiChatbotBackend.Models;
using Google.Cloud.Vision.V1;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SchoolAiChatbotBackend.Services
{
    public class OcrService : IOcrService
    {
        private readonly ILogger<OcrService> _logger;
        private readonly IMathOcrNormalizer _mathNormalizer;
        private readonly IConfiguration _configuration;
        private readonly ImageAnnotatorClient _visionClient;
        private readonly IFileStorageService _fileStorageService;

        public OcrService(
            ILogger<OcrService> logger,
            IMathOcrNormalizer mathNormalizer,
            IConfiguration configuration,
            IFileStorageService fileStorageService)
        {
            _logger = logger;
            _mathNormalizer = mathNormalizer;
            _configuration = configuration;
            _fileStorageService = fileStorageService;
            
            // Initialize Google Cloud Vision client
            try
            {
                // Try to get credentials from base64 environment variable (for Azure)
                var credentialsBase64 = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_BASE64");
                
                if (!string.IsNullOrEmpty(credentialsBase64))
                {
                    // Decode and write credentials to a temporary file
                    var credentialsJson = Encoding.UTF8.GetString(Convert.FromBase64String(credentialsBase64));
                    var tempPath = Path.Combine(Path.GetTempPath(), "google-credentials.json");
                    File.WriteAllText(tempPath, credentialsJson);
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempPath);
                    _logger.LogInformation("Google Cloud credentials configured from base64 environment variable");
                }
                else
                {
                    // Try to get credentials from JSON environment variable
                    var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
                    
                    if (!string.IsNullOrEmpty(credentialsJson))
                    {
                        // Write credentials to a temporary file
                        var tempPath = Path.Combine(Path.GetTempPath(), "google-credentials.json");
                        File.WriteAllText(tempPath, credentialsJson);
                        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempPath);
                        _logger.LogInformation("Google Cloud credentials configured from JSON environment variable");
                    }
                    else
                    {
                        // Fallback to file path (for local development)
                        var credentialsPath = _configuration["GoogleCloud:CredentialsPath"] 
                            ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                        
                        if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
                        {
                            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
                            _logger.LogInformation("Google Cloud credentials configured from: {Path}", credentialsPath);
                        }
                    }
                }
                
                _visionClient = ImageAnnotatorClient.Create();
                _logger.LogInformation("Google Cloud Vision client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Cloud Vision client. OCR will not be available.");
                _visionClient = null;
            }
        }

        public async Task<string> ExtractStudentAnswersTextAsync(WrittenSubmission submission)
        {
            _logger.LogInformation(
                "Starting OCR extraction for submission {SubmissionId} with {FileCount} files",
                submission.WrittenSubmissionId,
                submission.FilePaths.Count);

            var extractedText = new StringBuilder();

            foreach (var filePath in submission.FilePaths)
            {
                try
                {
                    string localFilePath = filePath;
                    bool isTemporaryFile = false;

                    // Check if this is an Azure Blob URL (starts with http/https)
                    if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                        filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Detected Azure Blob URL, downloading file: {BlobUrl}", filePath);
                        
                        // Download the blob to a temporary file
                        var tempFileName = $"{Guid.NewGuid()}{Path.GetExtension(filePath)}";
                        localFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                        
                        await _fileStorageService.DownloadFileAsync(filePath, localFilePath);
                        isTemporaryFile = true;
                        
                        _logger.LogInformation("Downloaded blob to temporary file: {TempPath}", localFilePath);
                    }

                    if (!File.Exists(localFilePath))
                    {
                        _logger.LogWarning("File not found: {FilePath}", localFilePath);
                        continue;
                    }

                    var fileExtension = Path.GetExtension(localFilePath).ToLowerInvariant();

                    if (fileExtension == ".pdf")
                    {
                        var text = await ExtractTextFromPdfAsync(localFilePath);
                        extractedText.AppendLine(text);
                    }
                    else if (IsImageFile(fileExtension))
                    {
                        var text = await ExtractTextFromImageAsync(localFilePath);
                        extractedText.AppendLine(text);
                    }
                    else
                    {
                        _logger.LogWarning("Unsupported file type: {Extension}", fileExtension);
                    }

                    // Clean up temporary file if we downloaded it
                    if (isTemporaryFile && File.Exists(localFilePath))
                    {
                        try
                        {
                            File.Delete(localFilePath);
                            _logger.LogInformation("Deleted temporary file: {TempPath}", localFilePath);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, "Failed to delete temporary file: {TempPath}", localFilePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting text from {FilePath}", filePath);
                }
            }

            var rawText = extractedText.ToString().Trim();

            // Normalize mathematical expressions in the OCR output
            _logger.LogInformation("Normalizing mathematical OCR output");
            var normalizedResult = await _mathNormalizer.NormalizeAsync(rawText);
            
            _logger.LogInformation(
                "OCR extraction completed for submission {SubmissionId}, extracted {CharCount} characters (modified: {WasModified})",
                submission.WrittenSubmissionId,
                normalizedResult.NormalizedAnswer.Length,
                normalizedResult.WasModified);

            return normalizedResult.NormalizedAnswer;
        }

        private async Task<string> ExtractTextFromImageAsync(string imagePath)
        {
            if (_visionClient == null)
            {
                _logger.LogError("Google Vision client not initialized. Cannot extract text from image: {ImagePath}", imagePath);
                return string.Empty;
            }

            try
            {
                _logger.LogInformation("Starting Google Vision OCR for image: {ImagePath}", imagePath);
                
                // Load image and send to Google Cloud Vision
                var image = await Google.Cloud.Vision.V1.Image.FromFileAsync(imagePath);
                var response = await _visionClient.DetectTextAsync(image);
                
                if (response == null || response.Count == 0)
                {
                    _logger.LogWarning("No text detected in image: {ImagePath}", imagePath);
                    return string.Empty;
                }

                // First annotation contains the entire detected text
                var fullText = response[0].Description;
                
                _logger.LogInformation(
                    "Google Vision OCR completed for {ImagePath}. Extracted {CharCount} characters",
                    Path.GetFileName(imagePath),
                    fullText?.Length ?? 0);
                
                return fullText ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google Vision OCR for image: {ImagePath}", imagePath);
                return string.Empty;
            }
        }

        private async Task<string> ExtractTextFromPdfAsync(string pdfPath)
        {
            try
            {
                _logger.LogInformation("Starting PDF text extraction: {PdfPath}", pdfPath);
                
                // First, try direct text extraction with PdfPig (for text-based PDFs)
                using var document = PdfDocument.Open(pdfPath);
                var extractedText = new StringBuilder();
                var hasText = false;

                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        extractedText.AppendLine(pageText);
                        hasText = true;
                    }
                }

                // If we extracted meaningful text, return it (text-based PDF)
                if (hasText && extractedText.Length > 50)
                {
                    _logger.LogInformation(
                        "PdfPig extracted {CharCount} characters from text-based PDF: {PdfPath}",
                        extractedText.Length,
                        Path.GetFileName(pdfPath));
                    return extractedText.ToString();
                }

                // If no text or very little text, treat as image-based/scanned PDF
                _logger.LogInformation(
                    "PDF appears to be image-based or scanned. Using Google Vision OCR: {PdfPath}",
                    pdfPath);
                
                return await ExtractTextFromImageBasedPdfAsync(pdfPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF: {PdfPath}", pdfPath);
                return string.Empty;
            }
        }

        private async Task<string> ExtractTextFromImageBasedPdfAsync(string pdfPath)
        {
            if (_visionClient == null)
            {
                _logger.LogError("Google Vision client not initialized. Cannot extract text from image-based PDF: {PdfPath}", pdfPath);
                return string.Empty;
            }

            try
            {
                _logger.LogInformation("Starting Google Vision OCR for image-based PDF: {PdfPath}", pdfPath);
                
                // For image-based PDFs, we can send the PDF directly to Google Vision
                // Google Vision supports PDF files and will process all pages
                var image = await Google.Cloud.Vision.V1.Image.FromFileAsync(pdfPath);
                var response = await _visionClient.DetectTextAsync(image);
                
                if (response == null || response.Count == 0)
                {
                    _logger.LogWarning("No text detected in image-based PDF: {PdfPath}", pdfPath);
                    return string.Empty;
                }

                // First annotation contains the entire detected text from all pages
                var fullText = response[0].Description;
                
                _logger.LogInformation(
                    "Google Vision OCR completed for image-based PDF: {PdfPath}. Extracted {CharCount} characters",
                    Path.GetFileName(pdfPath),
                    fullText?.Length ?? 0);
                
                return fullText ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google Vision OCR for image-based PDF: {PdfPath}", pdfPath);
                return string.Empty;
            }
        }

        private bool IsImageFile(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff" };
            return imageExtensions.Contains(extension);
        }
    }
}
