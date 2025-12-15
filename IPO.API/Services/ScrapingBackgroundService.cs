using IPO.API.Data;
using IPO.API.Models;

namespace IPO.API.Services
{
    /// <summary>
    /// Background service for scheduled IPO scraping
    /// </summary>
    public class ScrapingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScrapingBackgroundService> _logger;
        private Timer? _timer;

        public ScrapingBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ScrapingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraping Background Service starting");

            // Wait for app to fully start
            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var configRepo = scope.ServiceProvider.GetRequiredService<ConfigurationRepository>();

                    // Get refresh interval from configuration
                    var refreshInterval = await configRepo.GetIntValueAsync(
                        ConfigurationKeys.ScrapingRefreshInterval,
                        Constants.DefaultRefreshInterval);

                    // Check if auto-scraping is enabled (you can add this config)
                    var autoScrapingEnabled = await configRepo.GetBoolValueAsync(
                        "Scraping.AutoEnabled",
                        false);

                    if (autoScrapingEnabled)
                    {
                        _logger.LogInformation("Auto-scraping triggered");

                        var orchestrator = scope.ServiceProvider.GetRequiredService<ScrapingOrchestratorService>();

                        if (!orchestrator.IsRunning)
                        {
                            await orchestrator.StartScrapingAsync();
                        }
                        else
                        {
                            _logger.LogInformation("Scraping already in progress, skipping");
                        }
                    }

                    // Wait for next interval
                    await Task.Delay(TimeSpan.FromSeconds(refreshInterval), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scraping background service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Scraping Background Service stopping");
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scraping Background Service stopped");
            _timer?.Dispose();
            return base.StopAsync(stoppingToken);
        }
    }
}
