using Microsoft.AspNetCore.Mvc;
using IPO.API.Data;
using IPO.API.Models;

namespace IPO.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IPOController : ControllerBase
    {
        private readonly IPORepository _ipoRepository;
        private readonly IPOPremiumMappingRepository _mappingRepository;
        private readonly ILogger<IPOController> _logger;

        public IPOController(
            IPORepository ipoRepository,
            IPOPremiumMappingRepository mappingRepository,
            ILogger<IPOController> logger)
        {
            _ipoRepository = ipoRepository;
            _mappingRepository = mappingRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets all IPOs with optional filtering
        /// </summary>
        /// <param name="category">Filter by category (SME or Mainboard)</param>
        /// <param name="status">Filter by status</param>
        /// <param name="isActive">Filter by active status</param>
        /// <param name="search">Search by name</param>
        /// <returns>List of IPOs</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllIPOs(
            [FromQuery] string? category = null,
            [FromQuery] string? status = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null)
        {
            try
            {
                List<Models.IPO> ipos;

                if (!string.IsNullOrEmpty(search))
                {
                    ipos = await _ipoRepository.SearchByNameAsync(search);
                }
                else
                {
                    ipos = await _ipoRepository.GetAllAsync(category, status, isActive);
                }

                return Ok(new
                {
                    count = ipos.Count,
                    data = ipos,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IPOs");
                return StatusCode(500, new { message = "Error retrieving IPOs", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific IPO by ID
        /// </summary>
        /// <param name="id">IPO ID</param>
        /// <returns>IPO details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetIPO(int id)
        {
            try
            {
                var ipo = await _ipoRepository.GetByIdAsync(id);

                if (ipo == null)
                {
                    return NotFound(new { message = $"IPO with ID {id} not found" });
                }

                // Get mapping info
                var mapping = await _mappingRepository.GetByIPOIDAsync(id);

                return Ok(new
                {
                    ipo,
                    mapping,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IPO {Id}", id);
                return StatusCode(500, new { message = "Error retrieving IPO", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets IPO statistics
        /// </summary>
        /// <returns>Statistics by category</returns>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _ipoRepository.GetCategoryStatsAsync();

                return Ok(new
                {
                    categoryCounts = stats,
                    totalCount = stats.Values.Sum(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IPO statistics");
                return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes an IPO
        /// </summary>
        /// <param name="id">IPO ID to delete</param>
        /// <returns>Operation result</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIPO(int id)
        {
            try
            {
                var deleted = await _ipoRepository.DeleteAsync(id);

                if (!deleted)
                {
                    return NotFound(new { message = $"IPO with ID {id} not found" });
                }

                return Ok(new
                {
                    message = "IPO deleted successfully",
                    id,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting IPO {Id}", id);
                return StatusCode(500, new { message = "Error deleting IPO", error = ex.Message });
            }
        }
    }
}
