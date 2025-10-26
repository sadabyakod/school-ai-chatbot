using Microsoft.AspNetCore.Mvc;
using SchoolAiChatbotBackend.Data;
using System.Linq;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AnalyticsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] int schoolId)
        {
            // Placeholder: Return simple analytics
            var chatCount = _db.ChatLogs.Count(c => c.SchoolId == schoolId);
            var userCount = _db.Users.Count(u => u.SchoolId == schoolId);
            return Ok(new { chatCount, userCount });
        }
    }
}