using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Services;

public class InMemoryDataService
{
    private static readonly List<User> _users = new();
    private static readonly List<ChatLog> _chats = new();
    private static readonly List<Faq> _faqs = new()
    {
        new Faq 
        { 
            Id = 1,
            Question = "What are the school hours?", 
            Answer = "School hours are Monday-Friday 8:00 AM to 3:00 PM.", 
            Category = "General",
            CreatedAt = DateTime.UtcNow
        },
        new Faq 
        { 
            Id = 2,
            Question = "How do I contact the school?", 
            Answer = "You can contact us at (555) 123-4567 or email info@school.edu", 
            Category = "Contact",
            CreatedAt = DateTime.UtcNow
        },
        new Faq 
        { 
            Id = 3,
            Question = "What is the homework policy?", 
            Answer = "Homework should take approximately 10 minutes per grade level (e.g., 3rd grade = 30 minutes).", 
            Category = "Academic",
            CreatedAt = DateTime.UtcNow
        },
        new Faq 
        { 
            Id = 4,
            Question = "When is the next parent-teacher conference?", 
            Answer = "Parent-teacher conferences are scheduled for November 15-16, 2024. Please sign up through the school portal.", 
            Category = "Events",
            CreatedAt = DateTime.UtcNow
        }
    };

    // User operations
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await Task.FromResult(_users.FirstOrDefault(u => u.Email == email));
    }

    public async Task<User> CreateUserAsync(User user)
    {
        user.Id = _users.Count + 1;
        user.CreatedAt = DateTime.UtcNow;
        _users.Add(user);
        return await Task.FromResult(user);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }

    // Chat operations
    public async Task<ChatLog> SaveChatAsync(ChatLog chat)
    {
        chat.Id = _chats.Count + 1;
        chat.Timestamp = DateTime.UtcNow;
        _chats.Add(chat);
        return await Task.FromResult(chat);
    }

    public async Task<List<ChatLog>> GetUserChatsAsync(int userId, int limit = 10)
    {
        var userChats = _chats
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Timestamp)
            .Take(limit)
            .ToList();
        
        return await Task.FromResult(userChats);
    }

    // FAQ operations
    public async Task<List<Faq>> GetAllFaqsAsync()
    {
        return await Task.FromResult(_faqs.ToList());
    }

    public async Task<List<Faq>> SearchFaqsAsync(string query)
    {
        var results = _faqs
            .Where(f => f.Question.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       f.Answer.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        return await Task.FromResult(results);
    }

    // Analytics
    public async Task<int> GetTotalUsersAsync()
    {
        return await Task.FromResult(_users.Count);
    }

    public async Task<int> GetTotalChatsAsync()
    {
        return await Task.FromResult(_chats.Count);
    }

    public async Task<List<ChatLog>> GetRecentChatsAsync(int limit = 50)
    {
        var recentChats = _chats
            .OrderByDescending(c => c.Timestamp)
            .Take(limit)
            .ToList();
        
        return await Task.FromResult(recentChats);
    }
}