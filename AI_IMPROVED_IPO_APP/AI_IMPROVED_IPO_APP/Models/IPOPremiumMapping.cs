using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_IMPROVED_IPO_APP.Models
{
    /// <summary>
    /// Mapping table between internal IPO IDs and IPOpremium.in IDs
    /// </summary>
    public class IPOPremiumMapping
    {
        /// <summary>
        /// Unique identifier for the mapping
        /// </summary>
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// Foreign key to IPO table
        /// </summary>
        [Required]
        public int IPOID { get; set; }

        /// <summary>
        /// IPOpremium.in ID (from URL like /view/ipo/1092/)
        /// </summary>
        [Required]
        public int IPOPremiumID { get; set; }

        /// <summary>
        /// Slug from the URL (e.g., "marc-technocrats-ltd")
        /// </summary>
        [MaxLength(500)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Complete source URL
        /// </summary>
        [MaxLength(1000)]
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the mapping was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property to IPO
        /// </summary>
        [ForeignKey("IPOID")]
        public virtual IPO? IPO { get; set; }
    }
}
