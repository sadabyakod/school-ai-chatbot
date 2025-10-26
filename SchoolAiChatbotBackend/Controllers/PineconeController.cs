using Microsoft.AspNetCore.Mvc;
using SchoolAiChatbotBackend.Services;
using SchoolAiChatbotBackend.Models;
using System.Threading.Tasks;
using System.Text.Json;

namespace SchoolAiChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PineconeController : ControllerBase
    {
        private readonly PineconeService _pineconeService;

        public PineconeController(PineconeService pineconeService)
        {
            _pineconeService = pineconeService;
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestConnection()
        {
            var (ok, error) = await _pineconeService.TestConnectionAsync();
            if (ok)
                return Ok(new { status = "success", message = "Pinecone connection is working." });
            else
                return StatusCode(503, new { status = "error", message = "Pinecone connection failed.", details = error });
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertVectors([FromBody] PineconeUpsertRequest request)
        {
            // Ensure every vector's metadata includes model: 'llama-text-embed-v2'
            if (request?.Vectors != null)
            {
                foreach (var v in request.Vectors)
                {
                    if (v.Metadata == null)
                        v.Metadata = new Dictionary<string, object>();
                    v.Metadata["model"] = "llama-text-embed-v2";
                }
            }
            var (ok, result) = await _pineconeService.UpsertVectorsAsync(request);
            if (ok)
                return Ok(new { status = "success", message = "Vectors upserted.", details = result });
            else
                return StatusCode(500, new { status = "error", message = "Upsert failed.", details = result });
        }

        [HttpPost("query")]
        public async Task<IActionResult> QueryVectors([FromBody] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { status = "error", message = "Input ID cannot be null or empty." });
            }

            var (ok, result) = await _pineconeService.QueryVectorsAsync(new List<string> { id });

            if (ok && !string.IsNullOrWhiteSpace(result))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(result);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("vectors", out var vectors))
                    {
                        return Ok(new
                        {
                            status = "success",
                            message = "✅ Vector fetched successfully.",
                            vector = vectors
                        });
                    }

                    return Ok(new
                    {
                        status = "success",
                        message = "⚠️ No vector found for the given ID.",
                        details = result
                    });
                }
                catch (Exception ex)
                {
                    return Ok(new
                    {
                        status = "success",
                        message = "✅ Vector fetched (raw format).",
                        raw = result,
                        error = ex.Message
                    });
                }
            }
            else
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "❌ Query failed.",
                    details = result
                });
            }
        }
    }
}
