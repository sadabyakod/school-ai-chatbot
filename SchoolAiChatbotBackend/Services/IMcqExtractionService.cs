using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for extracting MCQ answers from uploaded answer sheet images
    /// </summary>
    public interface IMcqExtractionService
    {
        /// <summary>
        /// Extract MCQ answers from written submission images
        /// </summary>
        /// <param name="submission">Written submission containing image paths</param>
        /// <returns>Extracted MCQ answers</returns>
        Task<McqExtraction> ExtractMcqAnswersAsync(WrittenSubmission submission);

        /// <summary>
        /// Parse OCR text to identify MCQ answer patterns
        /// </summary>
        /// <param name="ocrText">Raw OCR extracted text</param>
        /// <returns>List of extracted MCQ answers</returns>
        List<ExtractedMcqAnswer> ParseMcqAnswers(string ocrText);
    }
}
