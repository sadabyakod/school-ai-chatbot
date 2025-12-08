using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    public class OcrService : IOcrService
    {
        private readonly ILogger<OcrService> _logger;
        private readonly IMathOcrNormalizer _mathNormalizer;

        public OcrService(
            ILogger<OcrService> logger,
            IMathOcrNormalizer mathNormalizer)
        {
            _logger = logger;
            _mathNormalizer = mathNormalizer;
        }

        public async Task<string> ExtractStudentAnswersTextAsync(WrittenSubmission submission)
        {
            // TODO: Integrate with Azure Computer Vision OCR or similar service
            // For now, this is a placeholder implementation
            
            _logger.LogInformation(
                "Starting OCR extraction for submission {SubmissionId} with {FileCount} files",
                submission.WrittenSubmissionId,
                submission.FilePaths.Count);

            var extractedText = new StringBuilder();

            foreach (var filePath in submission.FilePaths)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("File not found: {FilePath}", filePath);
                        continue;
                    }

                    var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

                    if (fileExtension == ".pdf")
                    {
                        var text = await ExtractTextFromPdfAsync(filePath);
                        extractedText.AppendLine(text);
                    }
                    else if (IsImageFile(fileExtension))
                    {
                        var text = await ExtractTextFromImageAsync(filePath);
                        extractedText.AppendLine(text);
                    }
                    else
                    {
                        _logger.LogWarning("Unsupported file type: {Extension}", fileExtension);
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
            // TODO: Integrate Azure Computer Vision OCR
            // Example integration code:
            /*
            var client = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(apiKey))
                { Endpoint = endpoint };

            using var stream = File.OpenRead(imagePath);
            var result = await client.RecognizePrintedTextInStreamAsync(true, stream);
            
            var text = new StringBuilder();
            foreach (var region in result.Regions)
            {
                foreach (var line in region.Lines)
                {
                    text.AppendLine(string.Join(" ", line.Words.Select(w => w.Text)));
                }
            }
            return text.ToString();
            */

            _logger.LogInformation("Simulating OCR extraction from image: {ImagePath}", imagePath);
            
            // Placeholder: Return simulated OCR text
            await Task.Delay(100); // Simulate processing delay
            
            return $"[Simulated OCR text from {Path.GetFileName(imagePath)}]\n" +
                   "Q1. Answer to question 1 goes here.\n" +
                   "Q2. Answer to question 2 goes here.\n";
        }

        private async Task<string> ExtractTextFromPdfAsync(string pdfPath)
        {
            // TODO: Integrate PDF text extraction library (e.g., PdfPig, iTextSharp)
            // Example code:
            /*
            using var document = PdfDocument.Open(pdfPath);
            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                text.AppendLine(page.Text);
            }
            return text.ToString();
            */

            _logger.LogInformation("Simulating text extraction from PDF: {PdfPath}", pdfPath);
            
            await Task.Delay(100);
            
            return $"[Simulated text from PDF: {Path.GetFileName(pdfPath)}]\n" +
                   "Q1. Student's answer for question 1.\n" +
                   "Q2. Student's answer for question 2.\n";
        }

        private bool IsImageFile(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff" };
            return imageExtensions.Contains(extension);
        }
    }
}
