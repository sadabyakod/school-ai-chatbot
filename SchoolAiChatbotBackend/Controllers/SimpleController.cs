using Microsoft.AspNetCore.Mvc;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimpleController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "Simple endpoint works!";
        }

        [HttpGet("ping")]
        public string Ping()
        {
            return "pong";
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok("Status OK");
        }
    }
}