using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolAiChatbotBackend.Controllers;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for evaluating subjective answers using AI
    /// </summary>
    public interface ISubjectiveEvaluator
    {
        /// <summary>
        /// Evaluates student's subjective answers using AI step-by-step analysis
        /// </summary>
        /// <param name="exam">The exam with all questions</param>
        /// <param name="studentAnswersRawText">OCR extracted text with all answers</param>
        /// <returns>List of evaluation results for each subjective question</returns>
        Task<List<SubjectiveEvaluationResult>> EvaluateSubjectiveAnswersAsync(
            GeneratedExamResponse exam, 
            string studentAnswersRawText);

        /// <summary>
        /// Evaluates student's subjective answers using stored rubrics for consistent marking.
        /// Fetches rubrics from database and uses them in the AI evaluation prompt.
        /// </summary>
        /// <param name="examId">Exam ID to fetch rubrics for</param>
        /// <param name="exam">The exam with all questions</param>
        /// <param name="studentAnswersRawText">OCR extracted text with all answers</param>
        /// <returns>List of evaluation results for each subjective question</returns>
        Task<List<SubjectiveEvaluationResult>> EvaluateWithRubricsAsync(
            string examId,
            GeneratedExamResponse exam, 
            string studentAnswersRawText);
    }
}

