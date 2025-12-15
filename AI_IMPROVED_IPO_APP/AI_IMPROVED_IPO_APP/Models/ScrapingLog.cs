using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_IMPROVED_IPO_APP.Models
{
    /// <summary>
    /// Scraping operation log entry
    /// </summary>
    public class ScrapingLog
    {
        /// <summary>
        /// Unique identifier for the log entry
        /// </summary>
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// Batch identifier for grouping related scraping operations
        /// </summary>
        public Guid BatchID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// IPOpremium.in ID being scraped
        /// </summary>
        public int? IPOPremiumID { get; set; }

        /// <summary>
        /// URL being scraped
        /// </summary>
        [MaxLength(1000)]
        public string? Url { get; set; }

        /// <summary>
        /// Current status of the scraping operation
        /// </summary>
        [Required]
        public ScrapingStatus Status { get; set; } = ScrapingStatus.Pending;

        /// <summary>
        /// Current step/phase of the operation
        /// </summary>
        [MaxLength(200)]
        public string? CurrentStep { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Stack trace if an exception occurred
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Number of retry attempts made
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Timestamp when the operation started
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Timestamp when the operation completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Duration of the operation in milliseconds
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// Timestamp when the log entry was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Foreign key to IPO table (if successfully created)
        /// </summary>
        public int? IPOID { get; set; }

        /// <summary>
        /// Navigation property to IPO
        /// </summary>
        [ForeignKey("IPOID")]
        public virtual IPO? IPO { get; set; }
    }

    /// <summary>
    /// Scraping operation status enumeration
    /// </summary>
    public enum ScrapingStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Retrying = 4,
        Cancelled = 5,
        Skipped = 6
    }

    /// <summary>
    /// Batch scraping summary
    /// </summary>
    public class ScrapingBatchSummary
    {
        public Guid BatchID { get; set; }
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double ProgressPercentage => TotalCount > 0 ? (CompletedCount * 100.0 / TotalCount) : 0;
        public bool IsCompleted => CompletedCount + FailedCount == TotalCount;
        public bool HasFailures => FailedCount > 0;
    }
}
