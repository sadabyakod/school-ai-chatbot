using Microsoft.AspNetCore.Mvc;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { 
                message = "Backend is working!", 
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        [HttpGet("config")]
        public IActionResult Config()
        {
            return Ok(new { 
                hasOpenAIKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OpenAI__ApiKey")),
                hasPineconeKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Pinecone__ApiKey")),
                hasJWTKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT__SecretKey")),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }
    }
}