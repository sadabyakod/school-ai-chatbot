using OpenAI;
using OpenAI.Chat;
using SchoolAiChatbot.Models;

namespace SchoolAiChatbot.Services;

public class OpenAiChatService
{
    private readonly ChatClient _chatClient;
    private readonly string _systemPrompt = @"
You are a helpful AI assistant for a school chatbot system. 
You provide accurate, helpful, and friendly responses to students, teachers, and parents.
Keep responses concise but informative.
If you don't know something specific about the school, suggest they contact the school directly.
";

    public OpenAiChatService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OpenAI API key not found");
        var openAiClient = new OpenAIClient(apiKey);
        _chatClient = openAiClient.GetChatClient("gpt-3.5-turbo");
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        try
        {
            var messages = new[]
            {
                ChatMessage.CreateSystemMessage(_systemPrompt),
                ChatMessage.CreateUserMessage(userMessage)
            };

            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            return "I apologize, but I'm having trouble processing your request right now. Please try again later or contact school support.";
        }
    }
}