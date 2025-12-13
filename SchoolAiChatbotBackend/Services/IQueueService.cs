namespace SchoolAiChatbotBackend.Services;

/// <summary>
/// Interface for queue operations - used to enqueue work for Azure Functions
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Enqueue a message to the specified queue
    /// </summary>
    Task EnqueueAsync<T>(string queueName, T message) where T : class;
    
    /// <summary>
    /// Enqueue a message with visibility delay
    /// </summary>
    Task EnqueueAsync<T>(string queueName, T message, TimeSpan visibilityDelay) where T : class;
}
