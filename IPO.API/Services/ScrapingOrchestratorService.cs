using IPO.API.Data;
using IPO.API.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IPO.API.Services
{
    /// <summary>
    /// Orchestrates the IPO scraping workflow with real-time progress tracking
    /// </summary>
    public class ScrapingOrchestratorService
    {
        private readonly ILogger<ScrapingOrchestratorService> _logger;
        private readonly SitemapParserService _sitemapParser;
        private readonly IPOScraperService _ipoScraper;
        private readonly IPORepository _ipoRepository;
        private readonly IPOPremiumMappingRepository _mappingRepository;
        private readonly ScrapingLogRepository _logRepository;
        private readonly ConfigurationRepository _configRepository;

        private bool _isRunning;
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<ScrapingProgressEventArgs>? ProgressChanged;
        public event EventHandler<ScrapingStatusEventArgs>? StatusChanged;

        public ScrapingOrchestratorService(
            ILogger<ScrapingOrchestratorService> logger,
            SitemapParserService sitemapParser,
            IPOScraperService ipoScraper,
            IPORepository ipoRepository,
            IPOPremiumMappingRepository mappingRepository,
            ScrapingLogRepository logRepository,
            ConfigurationRepository configRepository)
        {
            _logger = logger;
            _sitemapParser = sitemapParser;
            _ipoScraper = ipoScraper;
            _ipoRepository = ipoRepository;
            _mappingRepository = mappingRepository;
            _logRepository = logRepository;
            _configRepository = configRepository;
        }

        /// <summary>
        /// Starts the scraping process
        /// </summary>
        public async Task StartScrapingAsync()
        {
            if (_isRunning)
            {
                _logger.LogWarning("Scraping is already running");
                return;
            }

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await RunScrapingWorkflowAsync(_cancellationTokenSource.Token);
            }
            finally
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Stops the scraping process
        /// </summary>
        public void StopScraping()
        {
            if (_isRunning)
            {
                _logger.LogInformation("Stopping scraping process...");
                _cancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        /// Gets the current scraping status
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Main scraping workflow
        /// </summary>
        private async Task RunScrapingWorkflowAsync(CancellationToken cancellationToken)
        {
            var batchID = Guid.NewGuid();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                RaiseStatusChanged("Starting scraping workflow...", ScrapingStatus.InProgress);

                // Get configuration
                var config = await _configRepository.GetScrapingConfigAsync();
                var count = int.Parse(config["Count"]);
                var sitemapUrl = config["SitemapUrl"];
                var cardCssClass = config["CardCssClass"];
                var contentCssClass = config["ContentCssClass"];
                var retryCount = int.Parse(config["RetryCount"]);
                var retryDelay = int.Parse(config["RetryDelay"]);

                _logger.LogInformation("Starting scraping with BatchID: {BatchID}, Count: {Count}", batchID, count);

                // Step 1: Parse sitemap
                RaiseStatusChanged("Fetching sitemap...", ScrapingStatus.InProgress);
                var ipoUrls = await _sitemapParser.ParseSitemapAsync(sitemapUrl, count);

                if (ipoUrls.Count == 0)
                {
                    RaiseStatusChanged("No IPO URLs found in sitemap", ScrapingStatus.Failed);
                    return;
                }

                _logger.LogInformation("Found {Count} IPO URLs to scrape", ipoUrls.Count);

                // Step 2: Create scraping logs for each URL
                var logs = new List<ScrapingLog>();
                foreach (var urlInfo in ipoUrls)
                {
                    var log = new ScrapingLog
                    {
                        BatchID = batchID,
                        IPOPremiumID = urlInfo.IPOPremiumID,
                        Url = urlInfo.Url,
                        Status = ScrapingStatus.Pending
                    };
                    log.ID = await _logRepository.CreateAsync(log);
                    logs.Add(log);
                }

                // Step 3: Scrape each IPO
                int successCount = 0;
                int failureCount = 0;

                for (int i = 0; i < ipoUrls.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Scraping cancelled by user");
                        RaiseStatusChanged("Scraping cancelled", ScrapingStatus.Cancelled);
                        return;
                    }

                    var urlInfo = ipoUrls[i];
                    var log = logs[i];

                    try
                    {
                        // Check if already exists
                        var exists = await _mappingRepository.ExistsAsync(urlInfo.IPOPremiumID);
                        if (exists)
                        {
                            _logger.LogInformation("IPO {IPOPremiumID} already exists, skipping", urlInfo.IPOPremiumID);
                            log.Status = ScrapingStatus.Skipped;
                            log.CurrentStep = "Already exists";
                            await _logRepository.UpdateAsync(log);

                            RaiseProgressChanged(i + 1, ipoUrls.Count, urlInfo, log.Status);
                            continue;
                        }

                        // Update log status
                        log.Status = ScrapingStatus.InProgress;
                        log.StartedAt = DateTime.UtcNow;
                        log.CurrentStep = $"Scraping {urlInfo.Slug}";
                        await _logRepository.UpdateAsync(log);

                        RaiseStatusChanged($"Scraping {i + 1}/{ipoUrls.Count}: {urlInfo.Slug}", ScrapingStatus.InProgress);
                        RaiseProgressChanged(i + 1, ipoUrls.Count, urlInfo, ScrapingStatus.InProgress);

                        // Scrape IPO
                        var scrapedData = await _ipoScraper.ScrapeIPOAsync(
                            urlInfo,
                            cardCssClass,
                            contentCssClass,
                            retryCount,
                            retryDelay);

                        if (!scrapedData.Success)
                        {
                            // Scraping failed
                            log.Status = ScrapingStatus.Failed;
                            log.ErrorMessage = scrapedData.ErrorMessage;
                            log.StackTrace = scrapedData.StackTrace;
                            log.CompletedAt = DateTime.UtcNow;
                            log.DurationMs = scrapedData.ScrapeDurationMs;
                            await _logRepository.UpdateAsync(log);

                            failureCount++;

                            _logger.LogError("Failed to scrape IPO {IPOPremiumID}: {Error}",
                                urlInfo.IPOPremiumID, scrapedData.ErrorMessage);

                            RaiseProgressChanged(i + 1, ipoUrls.Count, urlInfo, ScrapingStatus.Failed);

                            // Stop entire process on failure as requested
                            RaiseStatusChanged($"Scraping failed at {urlInfo.Slug}: {scrapedData.ErrorMessage}", ScrapingStatus.Failed);
                            return;
                        }

                        // Save to database
                        var ipo = new IPO
                        {
                            Name = scrapedData.CompanyName,
                            CardHtml = scrapedData.CardHtml,
                            ContentHtml = scrapedData.ContentHtml,
                            Category = scrapedData.Category,
                            ScrapedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        ipo.ID = await _ipoRepository.CreateAsync(ipo);

                        // Create mapping
                        var mapping = new IPOPremiumMapping
                        {
                            IPOID = ipo.ID,
                            IPOPremiumID = urlInfo.IPOPremiumID,
                            Slug = urlInfo.Slug,
                            SourceUrl = urlInfo.Url
                        };
                        await _mappingRepository.CreateAsync(mapping);

                        // Update log
                        log.Status = ScrapingStatus.Completed;
                        log.IPOID = ipo.ID;
                        log.CompletedAt = DateTime.UtcNow;
                        log.DurationMs = scrapedData.ScrapeDurationMs;
                        await _logRepository.UpdateAsync(log);

                        successCount++;

                        _logger.LogInformation("Successfully scraped IPO {IPOPremiumID}: {Name}",
                            urlInfo.IPOPremiumID, scrapedData.CompanyName);

                        RaiseProgressChanged(i + 1, ipoUrls.Count, urlInfo, ScrapingStatus.Completed);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing IPO {IPOPremiumID}", urlInfo.IPOPremiumID);

                        log.Status = ScrapingStatus.Failed;
                        log.ErrorMessage = ex.Message;
                        log.StackTrace = ex.StackTrace;
                        log.CompletedAt = DateTime.UtcNow;
                        await _logRepository.UpdateAsync(log);

                        failureCount++;

                        RaiseProgressChanged(i + 1, ipoUrls.Count, urlInfo, ScrapingStatus.Failed);

                        // Stop on failure
                        RaiseStatusChanged($"Error processing {urlInfo.Slug}: {ex.Message}", ScrapingStatus.Failed);
                        return;
                    }
                }

                stopwatch.Stop();

                // All completed successfully
                var summary = $"Scraping completed! Success: {successCount}, Failed: {failureCount}, Duration: {stopwatch.Elapsed.TotalSeconds:F1}s";
                RaiseStatusChanged(summary, ScrapingStatus.Completed);

                _logger.LogInformation("Scraping workflow completed. BatchID: {BatchID}, Success: {Success}, Failed: {Failed}",
                    batchID, successCount, failureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in scraping workflow");
                RaiseStatusChanged($"Fatal error: {ex.Message}", ScrapingStatus.Failed);
            }
        }

        private void RaiseStatusChanged(string message, ScrapingStatus status)
        {
            StatusChanged?.Invoke(this, new ScrapingStatusEventArgs
            {
                Message = message,
                Status = status,
                Timestamp = DateTime.Now
            });
        }

        private void RaiseProgressChanged(int current, int total, IPOUrlInfo urlInfo, ScrapingStatus status)
        {
            ProgressChanged?.Invoke(this, new ScrapingProgressEventArgs
            {
                Current = current,
                Total = total,
                CurrentIPO = urlInfo,
                Status = status,
                ProgressPercentage = (current * 100.0) / total
            });
        }
    }

    public class ScrapingProgressEventArgs : EventArgs
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public IPOUrlInfo? CurrentIPO { get; set; }
        public ScrapingStatus Status { get; set; }
        public double ProgressPercentage { get; set; }
    }

    public class ScrapingStatusEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public ScrapingStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
