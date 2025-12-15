using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AI_IMPROVED_IPO_APP.Services
{
    /// <summary>
    /// Service for parsing IPO Premium sitemap and extracting IPO URLs
    /// </summary>
    public class SitemapParserService
    {
        private readonly ILogger<SitemapParserService> _logger;
        private readonly HttpClient _httpClient;

        public SitemapParserService(ILogger<SitemapParserService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Parses the sitemap and returns IPO URLs sorted by ID (ascending)
        /// </summary>
        /// <param name="sitemapUrl">The sitemap URL</param>
        /// <param name="count">Number of latest IPOs to return</param>
        /// <returns>List of IPO URLs with their IDs</returns>
        public async Task<List<IPOUrlInfo>> ParseSitemapAsync(string sitemapUrl, int count = 20)
        {
            try
            {
                _logger.LogInformation("Fetching sitemap from {Url}", sitemapUrl);

                var response = await _httpClient.GetAsync(sitemapUrl);
                response.EnsureSuccessStatusCode();

                var xmlContent = await response.Content.ReadAsStringAsync();
                var xdoc = XDocument.Parse(xmlContent);

                var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

                var urls = xdoc.Descendants(ns + "url")
                    .Select(url => url.Element(ns + "loc")?.Value)
                    .Where(loc => !string.IsNullOrEmpty(loc) && loc.Contains("/view/ipo/"))
                    .Select(loc => ParseIPOUrlInfo(loc!))
                    .Where(info => info != null)
                    .Cast<IPOUrlInfo>()
                    .OrderBy(info => info.IPOPremiumID)  // Sort ascending by ID
                    .ToList();

                _logger.LogInformation("Found {Count} IPO URLs in sitemap", urls.Count);

                // Take the last N (highest IDs)
                var latestUrls = urls.TakeLast(count).ToList();

                _logger.LogInformation("Returning {Count} latest IPO URLs", latestUrls.Count);

                return latestUrls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing sitemap from {Url}", sitemapUrl);
                throw;
            }
        }

        /// <summary>
        /// Parses an IPO URL and extracts ID and slug
        /// </summary>
        /// <param name="url">The IPO URL (e.g., https://www.ipopremium.in/view/ipo/1092/marc-technocrats-ltd)</param>
        /// <returns>IPOUrlInfo object with parsed details</returns>
        private IPOUrlInfo? ParseIPOUrlInfo(string url)
        {
            try
            {
                // Pattern: /view/ipo/{id}/{slug}
                var regex = new Regex(@"/view/ipo/(\d+)/([^/]+)");
                var match = regex.Match(url);

                if (match.Success && match.Groups.Count >= 3)
                {
                    var id = int.Parse(match.Groups[1].Value);
                    var slug = match.Groups[2].Value;

                    return new IPOUrlInfo
                    {
                        IPOPremiumID = id,
                        Slug = slug,
                        Url = url
                    };
                }

                _logger.LogWarning("Could not parse IPO URL: {Url}", url);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing IPO URL: {Url}", url);
                return null;
            }
        }

        /// <summary>
        /// Gets IPO URLs within a specific range
        /// </summary>
        /// <param name="sitemapUrl">The sitemap URL</param>
        /// <param name="startId">Start ID (inclusive)</param>
        /// <param name="endId">End ID (inclusive)</param>
        /// <returns>List of IPO URLs within the specified range</returns>
        public async Task<List<IPOUrlInfo>> GetIPOUrlsInRangeAsync(string sitemapUrl, int startId, int endId)
        {
            try
            {
                var allUrls = await ParseSitemapAsync(sitemapUrl, int.MaxValue);

                return allUrls
                    .Where(u => u.IPOPremiumID >= startId && u.IPOPremiumID <= endId)
                    .OrderBy(u => u.IPOPremiumID)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IPO URLs in range {StartId}-{EndId}", startId, endId);
                throw;
            }
        }
    }

    /// <summary>
    /// Information about an IPO URL
    /// </summary>
    public class IPOUrlInfo
    {
        public int IPOPremiumID { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
