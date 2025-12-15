using System.ComponentModel.DataAnnotations;

namespace IPO.API.Models
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Unique identifier for the configuration
        /// </summary>
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// Configuration key (unique)
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Configuration value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Description of the configuration
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Data type of the value (string, int, bool, etc.)
        /// </summary>
        [MaxLength(50)]
        public string DataType { get; set; } = "string";

        /// <summary>
        /// Category of the configuration
        /// </summary>
        [MaxLength(200)]
        public string Category { get; set; } = "General";

        /// <summary>
        /// Timestamp when the configuration was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the configuration was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuration keys as constants
    /// </summary>
    public static class ConfigurationKeys
    {
        public const string ScrapingCount = "Scraping.Count";
        public const string ScrapingRefreshInterval = "Scraping.RefreshInterval";
        public const string ScrapingSitemapUrl = "Scraping.SitemapUrl";
        public const string ScrapingCardCssClass = "Scraping.CardCssClass";
        public const string ScrapingContentCssClass = "Scraping.ContentCssClass";
        public const string ScrapingRetryCount = "Scraping.RetryCount";
        public const string ScrapingRetryDelay = "Scraping.RetryDelay";
        public const string ScrapingTimeout = "Scraping.Timeout";
        public const string DatabaseConnectionString = "Database.ConnectionString";
    }
}
