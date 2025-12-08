using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for extracting text from uploaded images/PDFs using OCR
    /// </summary>
    public interface IOcrService
    {
        /// <summary>
        /// Extracts text from student's uploaded answer files
        /// </summary>
        /// <param name="submission">The written submission with file paths</param>
        /// <returns>Combined OCR text from all files</returns>
        Task<string> ExtractStudentAnswersTextAsync(WrittenSubmission submission);
    }
}
