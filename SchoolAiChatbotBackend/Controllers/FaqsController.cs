using Microsoft.AspNetCore.Mvc;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Services;
using System.Linq;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaqsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FaqEmbeddingService _faqEmbeddingService;
        public FaqsController(AppDbContext db, FaqEmbeddingService faqEmbeddingService)
        {
            _db = db;
            _faqEmbeddingService = faqEmbeddingService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var faqs = _db.Faqs.ToList();
            return Ok(faqs);
        }

        [HttpPost("upsert-embeddings")]
        public async Task<IActionResult> UpsertEmbeddings([FromQuery] int schoolId)
        {
            await _faqEmbeddingService.UpsertFaqEmbeddingsAsync(schoolId);
            return Ok(new { message = "Embeddings upserted to Pinecone." });
        }
    }
}