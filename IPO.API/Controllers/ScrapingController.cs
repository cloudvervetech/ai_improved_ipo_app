using Microsoft.AspNetCore.Mvc;
using IPO.API.Services;
using IPO.API.Data;
using IPO.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace IPO.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScrapingController : ControllerBase
    {
        private readonly ScrapingOrchestratorService _orchestrator;
        private readonly ScrapingLogRepository _logRepository;
        private readonly IHubContext<ScrapingHub> _hubContext;
        private readonly ILogger<ScrapingController> _logger;

        public ScrapingController(
            ScrapingOrchestratorService orchestrator,
            ScrapingLogRepository logRepository,
            IHubContext<ScrapingHub> hubContext,
            ILogger<ScrapingController> logger)
        {
            _orchestrator = orchestrator;
            _logRepository = logRepository;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Starts the scraping process
        /// </summary>
        /// <returns>Operation result</returns>
        [HttpPost("start")]
        public async Task<IActionResult> StartScraping()
        {
            try
            {
                if (_orchestrator.IsRunning)
                {
                    return BadRequest(new { message = "Scraping is already running" });
                }

                // Start scraping in background
                _ = Task.Run(async () => await _orchestrator.StartScrapingAsync());

                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", new
                {
                    message = "Scraping started",
                    status = "InProgress",
                    timestamp = DateTime.UtcNow
                });

                return Ok(new
                {
                    message = "Scraping started successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting scraping");
                return StatusCode(500, new { message = "Error starting scraping", error = ex.Message });
            }
        }

        /// <summary>
        /// Stops the scraping process
        /// </summary>
        /// <returns>Operation result</returns>
        [HttpPost("stop")]
        public async Task<IActionResult> StopScraping()
        {
            try
            {
                if (!_orchestrator.IsRunning)
                {
                    return BadRequest(new { message = "Scraping is not running" });
                }

                _orchestrator.StopScraping();

                await _hubContext.Clients.All.SendAsync("ReceiveStatusUpdate", new
                {
                    message = "Scraping stopped",
                    status = "Cancelled",
                    timestamp = DateTime.UtcNow
                });

                return Ok(new
                {
                    message = "Scraping stop requested",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping scraping");
                return StatusCode(500, new { message = "Error stopping scraping", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the current scraping status
        /// </summary>
        /// <returns>Scraping status</returns>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var summary = await _logRepository.GetLatestBatchSummaryAsync();

                return Ok(new
                {
                    isRunning = _orchestrator.IsRunning,
                    currentBatch = summary,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scraping status");
                return StatusCode(500, new { message = "Error getting status", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets scraping history (recent batches)
        /// </summary>
        /// <param name="count">Number of recent batches to retrieve</param>
        /// <returns>List of batch summaries</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int count = 10)
        {
            try
            {
                var batches = await _logRepository.GetRecentBatchesAsync(count);
                var summaries = new List<object>();

                foreach (var batchId in batches)
                {
                    var summary = await _logRepository.GetBatchSummaryAsync(batchId);
                    if (summary != null)
                    {
                        summaries.Add(summary);
                    }
                }

                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scraping history");
                return StatusCode(500, new { message = "Error getting history", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets logs for a specific batch
        /// </summary>
        /// <param name="batchId">Batch GUID</param>
        /// <returns>List of scraping logs</returns>
        [HttpGet("batch/{batchId}/logs")]
        public async Task<IActionResult> GetBatchLogs(Guid batchId)
        {
            try
            {
                var logs = await _logRepository.GetByBatchAsync(batchId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch logs for {BatchId}", batchId);
                return StatusCode(500, new { message = "Error getting batch logs", error = ex.Message });
            }
        }
    }
}
