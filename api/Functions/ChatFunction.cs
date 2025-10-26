using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using SchoolAiChatbot.Models;
using SchoolAiChatbot.Services;

namespace SchoolAiChatbot.Functions;

public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    private readonly OpenAiChatService _chatService;

    public ChatFunction(ILogger<ChatFunction> logger)
    {
        _logger = logger;
        _chatService = new OpenAiChatService();
    }

    [Function("chat")]
    public async Task<HttpResponseData> PostChat(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequestData req)
    {
        _logger.LogInformation("Processing chat request");

        try
        {
            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (chatRequest == null || string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Message is required" }));
                return badResponse;
            }

            // Get AI response
            var aiReply = await _chatService.GetChatResponseAsync(chatRequest.Message);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var chatResponse = new ChatResponse
            {
                Reply = aiReply,
                Timestamp = DateTime.UtcNow,
                Source = "AI"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(chatResponse));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { 
                error = "Internal server error",
                message = "Sorry, I'm having trouble right now. Please try again later."
            }));
            return errorResponse;
        }
    }

    [Function("chat-options")]
    public HttpResponseData OptionsChat(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "chat")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        return response;
    }
}