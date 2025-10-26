using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;
using SchoolAiChatbotBackend.Services;

namespace SchoolAiChatbotBackend.Services
{
    public class FaqEmbeddingService
    {
        private readonly AppDbContext _dbContext;
    private readonly IChatService _openAiService;
        private readonly PineconeService _pineconeService;

        public FaqEmbeddingService(AppDbContext dbContext, IChatService openAiService, PineconeService pineconeService)
        {
            _dbContext = dbContext;
            _openAiService = openAiService;
            _pineconeService = pineconeService;
        }

        public async Task UpsertFaqEmbeddingsAsync(int schoolId)
        {
            var faqs = await _dbContext.Faqs.Where(f => f.SchoolId == schoolId).ToListAsync();
            var vectors = new List<PineconeVector>();
            foreach (var faq in faqs)
            {
                var text = faq.Question + " " + faq.Answer;
                var embedding = await _openAiService.GetEmbeddingAsync(text);
                vectors.Add(new PineconeVector
                {
                    Id = faq.Id.ToString(),
                    Values = embedding,
                    Metadata = new Dictionary<string, object>
                    {
                        { "faqId", faq.Id },
                        { "schoolId", faq.SchoolId },
                        { "question", faq.Question },
                        { "answer", faq.Answer }
                    }
                });
            }
            var request = new PineconeUpsertRequest { Vectors = vectors };
            await _pineconeService.UpsertVectorsAsync(request);
        }

        public async Task<string> FindClosestFaqAnswerAsync(string inputQuestion)
        {
            // Normalize input question: trim, lowercase, remove punctuation
            var normInput = new string(inputQuestion.Trim().ToLower().Where(c => !char.IsPunctuation(c)).ToArray());

            // Get all FAQs from the database
            var faqs = await _dbContext.Faqs
            .Where(f => f.Question != null)
            .ToListAsync();

            // Loosened matching: match if any word in input appears in FAQ question
            var inputWords = normInput.Split(' ').Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
            var faq = faqs.FirstOrDefault(f => {
                var normFaq = new string(f.Question.Trim().ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
                var faqWords = normFaq.Split(' ').Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
                return inputWords.Any(word => faqWords.Contains(word)) || faqWords.Any(word => inputWords.Contains(word));
            });
            return faq?.Answer ?? "No answer found.";
        }
    }
}
