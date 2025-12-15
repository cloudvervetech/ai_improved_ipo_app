using System.ComponentModel.DataAnnotations;

namespace AI_IMPROVED_IPO_APP.Models
{
    /// <summary>
    /// Represents an IPO (Initial Public Offering) entity
    /// </summary>
    public class IPO
    {
        /// <summary>
        /// Unique identifier for the IPO
        /// </summary>
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// Name of the company
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Scraped HTML content from card card-primary card-outline div
        /// </summary>
        public string? CardHtml { get; set; }

        /// <summary>
        /// Scraped HTML content from col-md-8 order-1 div
        /// </summary>
        public string? ContentHtml { get; set; }

        /// <summary>
        /// Category: SME or Mainboard
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; } = "Mainboard";

        /// <summary>
        /// Timestamp when the data was scraped
        /// </summary>
        public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Status of the IPO (Active, Closed, Upcoming, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? Status { get; set; }

        /// <summary>
        /// Issue price information
        /// </summary>
        [MaxLength(200)]
        public string? IssuePrice { get; set; }

        /// <summary>
        /// Lot size information
        /// </summary>
        public int? LotSize { get; set; }

        /// <summary>
        /// Opening date of the IPO
        /// </summary>
        public DateTime? OpenDate { get; set; }

        /// <summary>
        /// Closing date of the IPO
        /// </summary>
        public DateTime? CloseDate { get; set; }

        /// <summary>
        /// Issue size (in Crores)
        /// </summary>
        [MaxLength(200)]
        public string? IssueSize { get; set; }

        /// <summary>
        /// Indicates if the record is active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
