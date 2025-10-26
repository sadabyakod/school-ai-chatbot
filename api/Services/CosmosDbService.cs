using Microsoft.Azure.Cosmos;
using SchoolAiChatbot.Models;
using System.Net;

namespace SchoolAiChatbot.Services;

public class CosmosDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly Container _usersContainer;
    private readonly Container _chatsContainer;
    private readonly Container _faqsContainer;

    public CosmosDbService()
    {
        var connectionString = Environment.GetEnvironmentVariable("COSMOS_DB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            // For local development, use in-memory storage
            _cosmosClient = null!;
            return;
        }

        _cosmosClient = new CosmosClient(connectionString);
        _database = _cosmosClient.GetDatabase("SchoolAiApp");
        _usersContainer = _database.GetContainer("Users");
        _chatsContainer = _database.GetContainer("Chats");
        _faqsContainer = _database.GetContainer("FAQs");
    }

    // User operations
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        if (_cosmosClient == null) return InMemoryStorage.GetUserByEmail(email);

        try
        {
            var query = $"SELECT * FROM c WHERE c.Email = '{email}'";
            var queryDefinition = new QueryDefinition(query);
            var results = _usersContainer.GetItemQueryIterator<User>(queryDefinition);
            
            while (results.HasMoreResults)
            {
                var response = await results.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return null;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<User> CreateUserAsync(User user)
    {
        if (_cosmosClient == null) return InMemoryStorage.CreateUser(user);

        var response = await _usersContainer.CreateItemAsync(user, new PartitionKey(user.Email));
        return response.Resource;
    }

    public async Task<ChatLog> SaveChatAsync(ChatLog chat)
    {
        if (_cosmosClient == null) return InMemoryStorage.SaveChat(chat);

        var response = await _chatsContainer.CreateItemAsync(chat, new PartitionKey(chat.UserId));
        return response.Resource;
    }

    public async Task<List<ChatLog>> GetUserChatsAsync(string userId, int limit = 10)
    {
        if (_cosmosClient == null) return InMemoryStorage.GetUserChats(userId, limit);

        var query = $"SELECT * FROM c WHERE c.UserId = '{userId}' ORDER BY c.Timestamp DESC OFFSET 0 LIMIT {limit}";
        var queryDefinition = new QueryDefinition(query);
        var results = _chatsContainer.GetItemQueryIterator<ChatLog>(queryDefinition);
        
        var chats = new List<ChatLog>();
        while (results.HasMoreResults)
        {
            var response = await results.ReadNextAsync();
            chats.AddRange(response);
        }
        return chats;
    }

    public async Task<List<Faq>> GetFaqsAsync()
    {
        if (_cosmosClient == null) return InMemoryStorage.GetFaqs();

        var query = "SELECT * FROM c";
        var queryDefinition = new QueryDefinition(query);
        var results = _faqsContainer.GetItemQueryIterator<Faq>(queryDefinition);
        
        var faqs = new List<Faq>();
        while (results.HasMoreResults)
        {
            var response = await results.ReadNextAsync();
            faqs.AddRange(response);
        }
        return faqs;
    }
}

// In-memory storage for local development and free deployment
public static class InMemoryStorage
{
    private static readonly List<User> _users = new();
    private static readonly List<ChatLog> _chats = new();
    private static readonly List<Faq> _faqs = new()
    {
        new Faq { Question = "What are the school hours?", Answer = "School hours are Monday-Friday 8:00 AM to 3:00 PM.", Category = "General" },
        new Faq { Question = "How do I contact the school?", Answer = "You can contact us at (555) 123-4567 or email info@school.edu", Category = "Contact" },
        new Faq { Question = "What is the homework policy?", Answer = "Homework should take approximately 10 minutes per grade level (e.g., 3rd grade = 30 minutes).", Category = "Academic" }
    };

    public static User? GetUserByEmail(string email) => _users.FirstOrDefault(u => u.Email == email);
    
    public static User CreateUser(User user)
    {
        _users.Add(user);
        return user;
    }
    
    public static ChatLog SaveChat(ChatLog chat)
    {
        _chats.Add(chat);
        return chat;
    }
    
    public static List<ChatLog> GetUserChats(string userId, int limit) => 
        _chats.Where(c => c.UserId == userId).OrderByDescending(c => c.Timestamp).Take(limit).ToList();
    
    public static List<Faq> GetFaqs() => _faqs.ToList();
}