using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for extracting MCQ answers from uploaded answer sheet images using OCR
    /// </summary>
    public class McqExtractionService : IMcqExtractionService
    {
        private readonly IOcrService _ocrService;
        private readonly ILogger<McqExtractionService> _logger;

        // Regex patterns for detecting MCQ answers
        private static readonly Regex[] AnswerPatterns = new[]
        {
            // Pattern 1: "1) A", "2) B", "3) C", "Q1) A"
            new Regex(@"(?:Q|q)?(\d+)\s*\)\s*([A-Da-d])\b", RegexOptions.Compiled),
            
            // Pattern 2: "1. A", "2. B", "Q1. C"
            new Regex(@"(?:Q|q)?(\d+)\s*\.\s*([A-Da-d])\b", RegexOptions.Compiled),
            
            // Pattern 3: "1: A", "2: B", "Q1: D"
            new Regex(@"(?:Q|q)?(\d+)\s*:\s*([A-Da-d])\b", RegexOptions.Compiled),
            
            // Pattern 4: "1-A", "2-B", "Q1-C"
            new Regex(@"(?:Q|q)?(\d+)\s*-\s*([A-Da-d])\b", RegexOptions.Compiled),
            
            // Pattern 5: "Q1 A", "Q2 B", "1 A", "2 B"
            new Regex(@"(?:Q|q)?(\d+)\s+([A-Da-d])\b", RegexOptions.Compiled),
            
            // Pattern 6: Answer key format "1-A, 2-B, 3-C"
            new Regex(@"(\d+)\s*-\s*([A-Da-d])(?:\s*,|\s+)", RegexOptions.Compiled)
        };

        public McqExtractionService(
            IOcrService ocrService,
            ILogger<McqExtractionService> logger)
        {
            _ocrService = ocrService;
            _logger = logger;
        }

        public async Task<McqExtraction> ExtractMcqAnswersAsync(WrittenSubmission submission)
        {
            _logger.LogInformation(
                "Starting MCQ extraction for submission {SubmissionId}",
                submission.WrittenSubmissionId);

            var extraction = new McqExtraction
            {
                WrittenSubmissionId = submission.WrittenSubmissionId,
                ExamId = submission.ExamId,
                StudentId = submission.StudentId,
                Status = ExtractionStatus.Processing
            };

            try
            {
                // Extract text from all images using existing OCR service
                string ocrText = await _ocrService.ExtractStudentAnswersTextAsync(submission);
                extraction.RawOcrText = ocrText;

                _logger.LogInformation(
                    "OCR completed for submission {SubmissionId}, extracting MCQ answers",
                    submission.WrittenSubmissionId);

                // Parse MCQ answers from OCR text
                extraction.ExtractedAnswers = ParseMcqAnswers(ocrText);
                extraction.Status = ExtractionStatus.Completed;

                _logger.LogInformation(
                    "MCQ extraction completed for submission {SubmissionId}, found {Count} answers",
                    submission.WrittenSubmissionId,
                    extraction.ExtractedAnswers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error extracting MCQ answers for submission {SubmissionId}",
                    submission.WrittenSubmissionId);
                extraction.Status = ExtractionStatus.Failed;
            }

            return extraction;
        }

        public List<ExtractedMcqAnswer> ParseMcqAnswers(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("Empty OCR text provided for MCQ parsing");
                return new List<ExtractedMcqAnswer>();
            }

            var extractedAnswers = new Dictionary<int, ExtractedMcqAnswer>();

            // Try each pattern
            foreach (var pattern in AnswerPatterns)
            {
                var matches = pattern.Matches(ocrText);
                
                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        if (int.TryParse(match.Groups[1].Value, out int questionNumber))
                        {
                            var option = match.Groups[2].Value.ToUpperInvariant();

                            // Only consider valid options A, B, C, D
                            if (option == "A" || option == "B" || option == "C" || option == "D")
                            {
                                // Use the first occurrence of each question number
                                if (!extractedAnswers.ContainsKey(questionNumber))
                                {
                                    extractedAnswers[questionNumber] = new ExtractedMcqAnswer
                                    {
                                        QuestionNumber = questionNumber,
                                        ExtractedOption = option,
                                        Confidence = 1.0 // Can be enhanced with ML confidence scores
                                    };

                                    _logger.LogDebug(
                                        "Extracted answer: Q{QuestionNumber} = {Option}",
                                        questionNumber,
                                        option);
                                }
                            }
                        }
                    }
                }

                // If we found answers with this pattern, don't try other patterns
                // This prevents duplicate extraction with different patterns
                if (extractedAnswers.Count > 0)
                {
                    break;
                }
            }

            // Sort by question number
            var result = extractedAnswers.Values
                .OrderBy(a => a.QuestionNumber)
                .ToList();

            _logger.LogInformation(
                "Parsed {Count} MCQ answers from OCR text using pattern matching",
                result.Count);

            return result;
        }
    }
}
