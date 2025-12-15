using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AI_IMPROVED_IPO_APP.Services;
using AI_IMPROVED_IPO_APP.Data;
using AI_IMPROVED_IPO_APP.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace AI_IMPROVED_IPO_APP.PageModels
{
    public partial class ScrapingDashboardPageModel : ObservableObject
    {
        private readonly ScrapingOrchestratorService _orchestrator;
        private readonly DatabaseInitializer _dbInitializer;
        private readonly ScrapingLogRepository _logRepository;
        private readonly ILogger<ScrapingDashboardPageModel> _logger;

        [ObservableProperty]
        private string statusMessage = "Ready to start scraping";

        [ObservableProperty]
        private double progressPercentage = 0;

        [ObservableProperty]
        private int currentItem = 0;

        [ObservableProperty]
        private int totalItems = 0;

        [ObservableProperty]
        private bool isScrapingRunning = false;

        [ObservableProperty]
        private string currentIPOName = string.Empty;

        [ObservableProperty]
        private ScrapingStatus currentStatus = ScrapingStatus.Pending;

        [ObservableProperty]
        private ObservableCollection<ScrapingLogEntry> recentLogs = new();

        [ObservableProperty]
        private bool isDatabaseConnected = false;

        public ScrapingDashboardPageModel(
            ScrapingOrchestratorService orchestrator,
            DatabaseInitializer dbInitializer,
            ScrapingLogRepository logRepository,
            ILogger<ScrapingDashboardPageModel> logger)
        {
            _orchestrator = orchestrator;
            _dbInitializer = dbInitializer;
            _logRepository = logRepository;
            _logger = logger;

            // Subscribe to orchestrator events
            _orchestrator.ProgressChanged += OnProgressChanged;
            _orchestrator.StatusChanged += OnStatusChanged;

            // Initialize database connection test
            _ = TestDatabaseConnectionAsync();
        }

        [RelayCommand]
        private async Task StartScrapingAsync()
        {
            try
            {
                if (IsScrapingRunning)
                {
                    await AppShell.DisplayToastAsync("Scraping is already running");
                    return;
                }

                if (!IsDatabaseConnected)
                {
                    await AppShell.DisplaySnackbarAsync("Database not connected. Please check your connection string.");
                    return;
                }

                RecentLogs.Clear();
                StatusMessage = "Initializing scraping...";
                IsScrapingRunning = true;

                // Run scraping in background
                await Task.Run(async () => await _orchestrator.StartScrapingAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting scraping");
                StatusMessage = $"Error: {ex.Message}";
                await AppShell.DisplaySnackbarAsync($"Error starting scraping: {ex.Message}");
            }
            finally
            {
                IsScrapingRunning = false;
            }
        }

        [RelayCommand]
        private void StopScraping()
        {
            try
            {
                _orchestrator.StopScraping();
                StatusMessage = "Stopping scraping...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping scraping");
            }
        }

        [RelayCommand]
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                StatusMessage = "Initializing database...";
                await _dbInitializer.InitializeDatabaseAsync();
                StatusMessage = "Database initialized successfully!";
                IsDatabaseConnected = true;
                await AppShell.DisplayToastAsync("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                StatusMessage = $"Database initialization failed: {ex.Message}";
                await AppShell.DisplaySnackbarAsync($"Database error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task TestDatabaseConnectionAsync()
        {
            try
            {
                var isConnected = await _dbInitializer.TestConnectionAsync();
                IsDatabaseConnected = isConnected;

                if (isConnected)
                {
                    StatusMessage = "Database connected";
                }
                else
                {
                    StatusMessage = "Database not connected - Please check settings";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                IsDatabaseConnected = false;
                StatusMessage = "Database connection failed";
            }
        }

        [RelayCommand]
        private async Task ViewLogsAsync()
        {
            try
            {
                var batches = await _logRepository.GetRecentBatchesAsync(1);
                if (batches.Count > 0)
                {
                    var logs = await _logRepository.GetByBatchAsync(batches[0]);
                    RecentLogs.Clear();
                    foreach (var log in logs.OrderByDescending(l => l.CreatedAt).Take(20))
                    {
                        RecentLogs.Add(new ScrapingLogEntry
                        {
                            IPOName = log.Url?.Split('/').LastOrDefault() ?? "Unknown",
                            Status = log.Status.ToString(),
                            Message = log.ErrorMessage ?? log.CurrentStep ?? "Success",
                            Timestamp = log.CreatedAt.ToString("HH:mm:ss")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading logs");
            }
        }

        private void OnProgressChanged(object? sender, ScrapingProgressEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentItem = e.Current;
                TotalItems = e.Total;
                ProgressPercentage = e.ProgressPercentage;
                CurrentIPOName = e.CurrentIPO?.Slug ?? string.Empty;
                CurrentStatus = e.Status;

                // Add to logs
                if (e.CurrentIPO != null)
                {
                    RecentLogs.Insert(0, new ScrapingLogEntry
                    {
                        IPOName = e.CurrentIPO.Slug,
                        Status = e.Status.ToString(),
                        Message = $"IPO ID: {e.CurrentIPO.IPOPremiumID}",
                        Timestamp = DateTime.Now.ToString("HH:mm:ss")
                    });

                    // Keep only last 20 logs
                    while (RecentLogs.Count > 20)
                    {
                        RecentLogs.RemoveAt(RecentLogs.Count - 1);
                    }
                }
            });
        }

        private void OnStatusChanged(object? sender, ScrapingStatusEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = e.Message;
                CurrentStatus = e.Status;

                if (e.Status == ScrapingStatus.Completed || e.Status == ScrapingStatus.Failed || e.Status == ScrapingStatus.Cancelled)
                {
                    IsScrapingRunning = false;
                }
            });
        }
    }

    public class ScrapingLogEntry
    {
        public string IPOName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}
