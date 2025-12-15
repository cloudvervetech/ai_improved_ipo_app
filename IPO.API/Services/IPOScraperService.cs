using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using IPO.API.Models;
using System.Diagnostics;

namespace IPO.API.Services
{
    /// <summary>
    /// Service for scraping IPO details from web pages
    /// </summary>
    public class IPOScraperService
    {
        private readonly ILogger<IPOScraperService> _logger;
        private readonly HttpClient _httpClient;

        public IPOScraperService(ILogger<IPOScraperService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Scrapes IPO details from a URL with retry logic
        /// </summary>
        /// <param name="urlInfo">IPO URL information</param>
        /// <param name="cardCssClass">CSS class for card element</param>
        /// <param name="contentCssClass">CSS class for content element</param>
        /// <param name="retryCount">Number of retry attempts</param>
        /// <param name="retryDelay">Delay between retries in milliseconds</param>
        /// <returns>Scraped IPO data</returns>
        public async Task<IPOScrapedData> ScrapeIPOAsync(
            IPOUrlInfo urlInfo,
            string cardCssClass = "card card-primary card-outline",
            string contentCssClass = "col-md-8 order-1",
            int retryCount = 3,
            int retryDelay = 2000)
        {
            Exception? lastException = null;
            var stopwatch = Stopwatch.StartNew();

            for (int attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retry attempt {Attempt} for {Url}", attempt, urlInfo.Url);
                        await Task.Delay(retryDelay * attempt); // Exponential backoff
                    }

                    _logger.LogInformation("Scraping IPO {IPOPremiumID} from {Url}", urlInfo.IPOPremiumID, urlInfo.Url);

                    var html = await FetchHtmlAsync(urlInfo.Url);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(html);

                    var cardHtml = ExtractElementByClass(htmlDoc, cardCssClass);
                    var contentHtml = ExtractElementByClass(htmlDoc, contentCssClass);

                    if (string.IsNullOrEmpty(cardHtml) && string.IsNullOrEmpty(contentHtml))
                    {
                        throw new Exception($"No content found with classes '{cardCssClass}' or '{contentCssClass}'");
                    }

                    // Detect category (SME vs Mainboard)
                    var category = DetectCategory(html, cardHtml, contentHtml);

                    // Extract company name
                    var companyName = ExtractCompanyName(htmlDoc, urlInfo.Slug);

                    stopwatch.Stop();

                    return new IPOScrapedData
                    {
                        IPOPremiumID = urlInfo.IPOPremiumID,
                        Slug = urlInfo.Slug,
                        Url = urlInfo.Url,
                        CompanyName = companyName,
                        CardHtml = cardHtml,
                        ContentHtml = contentHtml,
                        Category = category,
                        ScrapeDurationMs = stopwatch.ElapsedMilliseconds,
                        Success = true
                    };
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Error scraping IPO {IPOPremiumID} (attempt {Attempt}/{MaxAttempts})",
                        urlInfo.IPOPremiumID, attempt + 1, retryCount + 1);
                }
            }

            stopwatch.Stop();

            // All retries failed
            return new IPOScrapedData
            {
                IPOPremiumID = urlInfo.IPOPremiumID,
                Slug = urlInfo.Slug,
                Url = urlInfo.Url,
                Success = false,
                ErrorMessage = lastException?.Message ?? "Unknown error",
                StackTrace = lastException?.StackTrace,
                ScrapeDurationMs = stopwatch.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// Fetches HTML content from a URL
        /// </summary>
        private async Task<string> FetchHtmlAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Extracts HTML element by CSS class
        /// </summary>
        private string? ExtractElementByClass(HtmlDocument doc, string cssClass)
        {
            try
            {
                // Try multiple selection strategies
                var element = FindElementByClass(doc.DocumentNode, cssClass);

                if (element != null)
                {
                    return element.OuterHtml;
                }

                _logger.LogWarning("Element with class '{CssClass}' not found", cssClass);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting element with class '{CssClass}'", cssClass);
                return null;
            }
        }

        /// <summary>
        /// Finds HTML element by class (supports multiple classes)
        /// </summary>
        private HtmlNode? FindElementByClass(HtmlNode node, string cssClass)
        {
            var classes = cssClass.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Try exact match first
            var xpath = $"//*[contains(concat(' ', normalize-space(@class), ' '), ' {string.Join(" ", classes)} ')]";
            var element = node.SelectSingleNode(xpath);

            if (element != null) return element;

            // Try matching all classes individually
            foreach (var cls in classes)
            {
                xpath = $"//*[contains(concat(' ', normalize-space(@class), ' '), ' {cls} ')]";
                element = node.SelectSingleNode(xpath);
                if (element != null) return element;
            }

            return null;
        }

        /// <summary>
        /// Detects if IPO is SME or Mainboard based on content
        /// </summary>
        private string DetectCategory(string fullHtml, string? cardHtml, string? contentHtml)
        {
            var combinedText = $"{fullHtml} {cardHtml} {contentHtml}".ToLower();

            var smeMatches = System.Text.RegularExpressions.Regex.Matches(combinedText, @"\bsme\b");
            var mainboardMatches = System.Text.RegularExpressions.Regex.Matches(combinedText, @"\bmainboard\b");

            _logger.LogDebug("SME matches: {SMECount}, Mainboard matches: {MainboardCount}",
                smeMatches.Count, mainboardMatches.Count);

            // If SME is mentioned more frequently, consider it SME
            if (smeMatches.Count > mainboardMatches.Count && smeMatches.Count > 0)
            {
                return "SME";
            }

            return "Mainboard";
        }

        /// <summary>
        /// Extracts company name from the page
        /// </summary>
        private string ExtractCompanyName(HtmlDocument doc, string fallbackSlug)
        {
            try
            {
                // Try multiple strategies to find company name
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                if (titleNode != null)
                {
                    var title = titleNode.InnerText.Trim();
                    // Extract company name from title (usually before "IPO" or "|")
                    var parts = title.Split(new[] { "IPO", "|", "-" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        return parts[0].Trim();
                    }
                }

                // Try h1 heading
                var h1Node = doc.DocumentNode.SelectSingleNode("//h1");
                if (h1Node != null)
                {
                    return h1Node.InnerText.Trim();
                }

                // Fallback: Convert slug to readable name
                return ConvertSlugToName(fallbackSlug);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting company name");
                return ConvertSlugToName(fallbackSlug);
            }
        }

        /// <summary>
        /// Converts URL slug to readable company name
        /// </summary>
        private string ConvertSlugToName(string slug)
        {
            return string.Join(" ", slug.Split('-'))
                .ToUpper()
                .Replace("LTD", "Ltd")
                .Replace("PVT", "Pvt");
        }
    }

    /// <summary>
    /// Data scraped from an IPO page
    /// </summary>
    public class IPOScrapedData
    {
        public int IPOPremiumID { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CardHtml { get; set; }
        public string? ContentHtml { get; set; }
        public string Category { get; set; } = "Mainboard";
        public long ScrapeDurationMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}
