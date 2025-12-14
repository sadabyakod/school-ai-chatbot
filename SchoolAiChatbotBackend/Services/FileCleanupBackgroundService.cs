using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SchoolAiChatbotBackend.Services
{
    /// <summary>
    /// Background service that periodically cleans up expired files based on retention policy
    /// Runs daily at 2 AM to delete/archive files older than configured retention period
    /// </summary>
    public class FileCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<FileCleanupBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily

        public FileCleanupBackgroundService(
            ILogger<FileCleanupBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Cleanup Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate delay until next 2 AM
                    var now = DateTime.Now;
                    var nextRun = now.Date.AddDays(1).AddHours(2); // Next day at 2 AM

                    if (now.Hour < 2)
                    {
                        // If before 2 AM today, run today at 2 AM
                        nextRun = now.Date.AddHours(2);
                    }

                    var delay = nextRun - now;
                    _logger.LogInformation(
                        "Next file cleanup scheduled at {NextRun} (in {Hours} hours, {Minutes} minutes)",
                        nextRun,
                        (int)delay.TotalHours,
                        delay.Minutes);

                    // Wait until next scheduled run
                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await PerformCleanupAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    _logger.LogInformation("File Cleanup Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in File Cleanup Background Service");
                    // Wait before retrying to avoid tight error loop
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }

        private async Task PerformCleanupAsync()
        {
            try
            {
                _logger.LogInformation("Starting scheduled file cleanup task");

                // Create a scope to resolve scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var fileStorageService = scope.ServiceProvider.GetService<IFileStorageService>();

                    if (fileStorageService is AzureBlobStorageService azureBlobService)
                    {
                        await azureBlobService.CleanupExpiredFilesAsync();
                        _logger.LogInformation("Azure Blob Storage cleanup completed successfully");
                    }
                    else
                    {
                        _logger.LogInformation("File cleanup skipped - not using Azure Blob Storage");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file cleanup task");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Cleanup Background Service stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
