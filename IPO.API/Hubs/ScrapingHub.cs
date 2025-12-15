using Microsoft.AspNetCore.SignalR;
using IPO.API.Models;

namespace IPO.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time scraping status updates
    /// </summary>
    public class ScrapingHub : Hub
    {
        private readonly ILogger<ScrapingHub> _logger;

        public ScrapingHub(ILogger<ScrapingHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Broadcasts scraping progress to all connected clients
        /// </summary>
        public async Task SendProgress(int current, int total, double percentage, string currentIPO, string status)
        {
            await Clients.All.SendAsync("ReceiveProgress", new
            {
                current,
                total,
                percentage,
                currentIPO,
                status,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Broadcasts scraping status change to all connected clients
        /// </summary>
        public async Task SendStatusUpdate(string message, string status)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", new
            {
                message,
                status,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Broadcasts scraping completion to all connected clients
        /// </summary>
        public async Task SendScrapingComplete(int successCount, int failedCount, int skippedCount, long durationMs)
        {
            await Clients.All.SendAsync("ReceiveScrapingComplete", new
            {
                successCount,
                failedCount,
                skippedCount,
                durationMs,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
