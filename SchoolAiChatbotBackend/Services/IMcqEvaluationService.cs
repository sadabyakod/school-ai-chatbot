using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.DTOs;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for evaluating MCQ answers extracted from answer sheets
    /// </summary>
    public interface IMcqEvaluationService
    {
        /// <summary>
        /// Evaluate extracted MCQ answers against exam questions
        /// </summary>
        /// <param name="extraction">Extracted MCQ answers</param>
        /// <param name="exam">Generated exam with correct answers</param>
        /// <returns>Evaluation results with scores</returns>
        Task<McqEvaluationFromSheet> EvaluateExtractedAnswersAsync(
            McqExtraction extraction,
            GeneratedExamResponse exam);
    }
}
