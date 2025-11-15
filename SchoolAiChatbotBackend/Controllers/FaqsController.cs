using Microsoft.AspNetCore.Mvc;
using SchoolAiChatbotBackend.Data;
using System.Linq;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaqsController : ControllerBase
    {
        private readonly AppDbContext _db;
        
        public FaqsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var faqs = _db.Faqs.ToList();
            return Ok(faqs);
        }
    }
}