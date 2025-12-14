using Azure.Storage.Queues;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SchoolAiChatbotBackend.Services;

/// <summary>
/// Production-hardened Azure Queue Storage implementation for enqueuing work to Azure Functions
/// Features: Connection caching, retry logic, structured logging
/// </summary>
public class AzureQueueService : IQueueService
{
    private readonly string _connectionString;
    private readonly ILogger<AzureQueueService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, QueueClient> _queueClients;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // Retry configuration
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = {
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1)
    };

    public AzureQueueService(string connectionString, ILogger<AzureQueueService> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queueClients = new ConcurrentDictionary<string, QueueClient>();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task EnqueueAsync<T>(string queueName, T message) where T : class
    {
        await EnqueueAsync(queueName, message, TimeSpan.Zero);
    }

    public async Task EnqueueAsync<T>(string queueName, T message, TimeSpan visibilityDelay) where T : class
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentNullException(nameof(queueName));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var startTime = DateTime.UtcNow;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var queueClient = await GetOrCreateQueueClientAsync(queueName);

                var jsonMessage = JsonSerializer.Serialize(message, _jsonOptions);
                var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonMessage));

                Azure.Response<Azure.Storage.Queues.Models.SendReceipt> response;
                if (visibilityDelay > TimeSpan.Zero)
                {
                    response = await queueClient.SendMessageAsync(base64Message, visibilityDelay);
                }
                else
                {
                    response = await queueClient.SendMessageAsync(base64Message);
                }

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogInformation(
                    "[QUEUE_SUCCESS] Enqueued {MessageType} to {QueueName} in {ElapsedMs}ms (attempt {Attempt})",
                    typeof(T).Name,
                    queueName,
                    elapsedMs,
                    attempt + 1);

                return; // Success!
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 503 || ex.Status == 429)
            {
                // Transient failure - retry
                lastException = ex;
                if (attempt < MaxRetries)
                {
                    _logger.LogWarning(
                        "[QUEUE_RETRY] Transient failure on {QueueName}, attempt {Attempt}/{MaxRetries}: {Error}",
                        queueName,
                        attempt + 1,
                        MaxRetries,
                        ex.Message);

                    await Task.Delay(RetryDelays[Math.Min(attempt, RetryDelays.Length - 1)]);
                }
            }
            catch (Exception ex)
            {
                // Non-transient failure - don't retry
                _logger.LogError(ex,
                    "[QUEUE_FAILED] Failed to enqueue {MessageType} to {QueueName}",
                    typeof(T).Name,
                    queueName);
                throw;
            }
        }

        // All retries exhausted
        _logger.LogError(lastException,
            "[QUEUE_EXHAUSTED] All {MaxRetries} retries failed for {QueueName}",
            MaxRetries,
            queueName);
        throw lastException ?? new InvalidOperationException("Queue operation failed");
    }

    /// <summary>
    /// Get or create a cached QueueClient for the specified queue
    /// </summary>
    private async Task<QueueClient> GetOrCreateQueueClientAsync(string queueName)
    {
        if (_queueClients.TryGetValue(queueName, out var existingClient))
        {
            return existingClient;
        }

        await _initLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_queueClients.TryGetValue(queueName, out existingClient))
            {
                return existingClient;
            }

            var queueClient = new QueueClient(_connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();

            _queueClients[queueName] = queueClient;
            _logger.LogInformation("[QUEUE_INIT] Created queue client for {QueueName}", queueName);

            return queueClient;
        }
        finally
        {
            _initLock.Release();
        }
    }
}

/// <summary>
/// In-memory queue service for local development/testing
/// </summary>
public class InMemoryQueueService : IQueueService
{
    private readonly ILogger<InMemoryQueueService> _logger;

    public InMemoryQueueService(ILogger<InMemoryQueueService> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync<T>(string queueName, T message) where T : class
    {
        _logger.LogWarning(
            "[DEV MODE] Message enqueued to {QueueName} (in-memory, no Azure Functions processing): {MessageType}",
            queueName,
            typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task EnqueueAsync<T>(string queueName, T message, TimeSpan visibilityDelay) where T : class
    {
        return EnqueueAsync(queueName, message);
    }
}
