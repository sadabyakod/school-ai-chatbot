using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Service for normalizing OCR output from handwritten mathematical answers
    /// Converts noisy OCR text into clean, evaluable mathematical notation
    /// </summary>
    public interface IMathOcrNormalizer
    {
        /// <summary>
        /// Normalize OCR text containing mathematical expressions
        /// </summary>
        /// <param name="ocrText">Raw OCR output with potential errors</param>
        /// <returns>Clean, normalized mathematical text</returns>
        Task<MathNormalizationResult> NormalizeAsync(string ocrText);
    }

    public class MathNormalizationResult
    {
        public string NormalizedAnswer { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public bool WasModified { get; set; }
    }

    public class MathOcrNormalizer : IMathOcrNormalizer
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<MathOcrNormalizer> _logger;

        // System prompt for AI-based math OCR normalization
        private const string MATH_NORMALIZER_SYSTEM_PROMPT = @"You are a Mathematical OCR Normalizer.

Your task:
- Convert noisy OCR output into perfect, clean mathematical text.
- Fix broken equations, symbols, spacing, and formatting.
- Reconstruct math expressions accurately.
- Identify and repair misread characters.
- Convert handwriting-like OCR output into clear LaTeX-style or plain math notation.
- DO NOT add steps or explanations. Only normalize what the student wrote.

Input OCR text may contain:
- Misread superscripts, subscripts
- Split letters or missing operators
- Wrong symbols (+ instead of x, x instead of ×, etc.)
- Broken terms like: ""2 x 2"", ""x^ 2"", ""∫ f( x dx"", ""lim x → ∞""

Rules for output:
- Keep EXACT meaning of the student's answer.
- Preserve all steps the student attempted.
- Fix notation only, not logic.
- Output clean, minimal math expression lines that AI can evaluate.

Return JSON only:
{
  ""normalizedAnswer"": ""string""
}

Example transformations:
- OCR: ""d dx x2  +3 x"" → OUTPUT: ""d/dx (x^2 + 3x)""
- OCR: ""∫ x 2 dx = x3 / 3 + c"" → OUTPUT: ""∫x^2 dx = x^3/3 + C""
- OCR: ""lim x → 0 sin x / x = 1"" → OUTPUT: ""lim(x→0) sin(x)/x = 1""
- OCR: ""det A = | 1 2 | = -2"" → OUTPUT: ""det(A) = |1 2; 3 4| = -2""
- OCR: ""f ' ( x ) = 2 x"" → OUTPUT: ""f'(x) = 2x""
- OCR: ""A ^ -1 = 1/det A * adj A"" → OUTPUT: ""A^(-1) = (1/det(A)) * adj(A)""";

        public MathOcrNormalizer(
            IOpenAIService openAIService,
            ILogger<MathOcrNormalizer> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        public async Task<MathNormalizationResult> NormalizeAsync(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
            {
                return new MathNormalizationResult
                {
                    NormalizedAnswer = string.Empty,
                    OriginalText = ocrText ?? string.Empty,
                    WasModified = false
                };
            }

            _logger.LogInformation("Normalizing OCR text ({Length} chars)", ocrText.Length);

            // First, apply rule-based normalization for common patterns
            var ruleBasedResult = ApplyRuleBasedNormalization(ocrText);

            // Then use AI for complex mathematical expression normalization
            try
            {
                var userPrompt = $"Normalize this OCR output:\n\n{ruleBasedResult}";
                var aiResponse = await _openAIService.EvaluateSubjectiveAnswerAsync(
                    MATH_NORMALIZER_SYSTEM_PROMPT,
                    userPrompt);

                // Parse the AI response
                var normalized = ExtractNormalizedAnswer(aiResponse);

                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    _logger.LogInformation("OCR normalization complete");
                    return new MathNormalizationResult
                    {
                        NormalizedAnswer = normalized,
                        OriginalText = ocrText,
                        WasModified = !normalized.Equals(ocrText, StringComparison.Ordinal)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI normalization failed, using rule-based result");
            }

            // Fallback to rule-based result
            return new MathNormalizationResult
            {
                NormalizedAnswer = ruleBasedResult,
                OriginalText = ocrText,
                WasModified = !ruleBasedResult.Equals(ocrText, StringComparison.Ordinal)
            };
        }

        /// <summary>
        /// Apply rule-based normalization for common OCR errors
        /// </summary>
        private string ApplyRuleBasedNormalization(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = text;

            // Fix common superscript patterns
            // "x 2" or "x2" → "x^2"
            result = Regex.Replace(result, @"([a-zA-Z])\s*(\d+)(?!\^)", "$1^$2");

            // Fix broken exponents: "x^ 2" → "x^2"
            result = Regex.Replace(result, @"\^\s+", "^");

            // Fix derivative notation: "d dx" → "d/dx"
            result = Regex.Replace(result, @"\bd\s+dx\b", "d/dx", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bd\s+dy\b", "d/dy", RegexOptions.IgnoreCase);

            // Fix function notation: "f ( x )" → "f(x)"
            result = Regex.Replace(result, @"([a-zA-Z])\s*\(\s*([a-zA-Z])\s*\)", "$1($2)");

            // Fix prime notation: "f ' ( x )" → "f'(x)"
            result = Regex.Replace(result, @"([a-zA-Z])\s*'\s*\(", "$1'(");

            // Fix limit notation: "lim x → 0" → "lim(x→0)"
            result = Regex.Replace(result, @"lim\s+([a-zA-Z])\s*(?:→|->)+\s*(\S+)", "lim($1→$2)");

            // Fix integral notation: "∫ f( x dx" → "∫f(x)dx"
            result = Regex.Replace(result, @"∫\s+", "∫");
            result = Regex.Replace(result, @"\(\s*([a-zA-Z])\s+dx", "($1)dx");

            // Fix matrix determinant: "det A" → "det(A)"
            result = Regex.Replace(result, @"\bdet\s+([A-Z])\b", "det($1)");

            // Fix inverse notation: "A ^ -1" → "A^(-1)"
            result = Regex.Replace(result, @"([A-Z])\s*\^\s*-\s*1\b", "$1^(-1)");

            // Fix fraction notation: spacing around /
            result = Regex.Replace(result, @"\s*/\s*", "/");

            // Fix common OCR misreads
            result = result.Replace("×", "*");
            result = result.Replace("÷", "/");
            result = result.Replace("−", "-"); // en-dash to minus
            result = result.Replace("–", "-"); // em-dash to minus

            // Fix spacing around operators
            result = Regex.Replace(result, @"\s*\+\s*", " + ");
            result = Regex.Replace(result, @"\s*-\s*", " - ");
            result = Regex.Replace(result, @"\s*=\s*", " = ");

            // Fix multiple spaces
            result = Regex.Replace(result, @"\s+", " ");

            // Trim
            result = result.Trim();

            return result;
        }

        /// <summary>
        /// Extract normalized answer from AI JSON response
        /// </summary>
        private string ExtractNormalizedAnswer(string jsonResponse)
        {
            try
            {
                // Try to parse as JSON
                var match = Regex.Match(jsonResponse, @"""normalizedAnswer""\s*:\s*""([^""]+)""");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                // Try parsing with System.Text.Json
                var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("normalizedAnswer", out var prop))
                {
                    return prop.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse normalization response");
            }

            return string.Empty;
        }
    }
}
