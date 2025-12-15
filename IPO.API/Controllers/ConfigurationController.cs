using Microsoft.AspNetCore.Mvc;
using IPO.API.Data;
using IPO.API.Models;

namespace IPO.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ConfigurationRepository _configRepository;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            ConfigurationRepository configRepository,
            ILogger<ConfigurationController> logger)
        {
            _configRepository = configRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets all configuration settings
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <returns>List of configuration settings</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? category = null)
        {
            try
            {
                var configs = await _configRepository.GetAllAsync(category);

                return Ok(new
                {
                    count = configs.Count,
                    data = configs,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configurations");
                return StatusCode(500, new { message = "Error retrieving configurations", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets scraping-specific configuration
        /// </summary>
        /// <returns>Scraping configuration</returns>
        [HttpGet("scraping")]
        public async Task<IActionResult> GetScrapingConfig()
        {
            try
            {
                var config = await _configRepository.GetScrapingConfigAsync();

                return Ok(new
                {
                    data = config,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scraping configuration");
                return StatusCode(500, new { message = "Error retrieving scraping configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific configuration value by key
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value</returns>
        [HttpGet("{key}")]
        public async Task<IActionResult> GetByKey(string key)
        {
            try
            {
                var value = await _configRepository.GetValueAsync(key);

                if (value == null)
                {
                    return NotFound(new { message = $"Configuration with key '{key}' not found" });
                }

                return Ok(new
                {
                    key,
                    value,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration {Key}", key);
                return StatusCode(500, new { message = "Error retrieving configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Updates a configuration value
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="request">Update request with new value</param>
        /// <returns>Operation result</returns>
        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateValue(string key, [FromBody] UpdateConfigRequest request)
        {
            try
            {
                var updated = await _configRepository.SetValueAsync(key, request.Value);

                if (!updated)
                {
                    return NotFound(new { message = $"Configuration with key '{key}' not found" });
                }

                return Ok(new
                {
                    message = "Configuration updated successfully",
                    key,
                    value = request.Value,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration {Key}", key);
                return StatusCode(500, new { message = "Error updating configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Clears the configuration cache
        /// </summary>
        /// <returns>Operation result</returns>
        [HttpPost("cache/clear")]
        public IActionResult ClearCache()
        {
            try
            {
                _configRepository.ClearCache();

                return Ok(new
                {
                    message = "Configuration cache cleared successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing configuration cache");
                return StatusCode(500, new { message = "Error clearing cache", error = ex.Message });
            }
        }
    }

    public class UpdateConfigRequest
    {
        public string Value { get; set; } = string.Empty;
    }
}
