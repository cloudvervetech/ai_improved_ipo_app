using AI_IMPROVED_IPO_APP.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AI_IMPROVED_IPO_APP.Data
{
    /// <summary>
    /// Repository for AppConfiguration operations
    /// </summary>
    public class ConfigurationRepository
    {
        private readonly ILogger<ConfigurationRepository> _logger;
        private readonly string _connectionString;
        private Dictionary<string, string> _cache = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public ConfigurationRepository(ILogger<ConfigurationRepository> logger, string? connectionString = null)
        {
            _logger = logger;
            _connectionString = connectionString ?? Constants.MSSQLConnectionString;
        }

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        public async Task<string?> GetValueAsync(string key)
        {
            // Try cache first
            if (_cache.ContainsKey(key) && DateTime.UtcNow - _lastCacheUpdate < _cacheExpiry)
            {
                return _cache[key];
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT [Value] FROM AppConfiguration WHERE [Key] = @Key",
                connection);
            cmd.Parameters.AddWithValue("@Key", key);

            var result = await cmd.ExecuteScalarAsync();
            var value = result?.ToString();

            if (value != null)
            {
                _cache[key] = value;
                _lastCacheUpdate = DateTime.UtcNow;
            }

            return value;
        }

        /// <summary>
        /// Gets a configuration value as int
        /// </summary>
        public async Task<int> GetIntValueAsync(string key, int defaultValue)
        {
            var value = await GetValueAsync(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets a configuration value as bool
        /// </summary>
        public async Task<bool> GetBoolValueAsync(string key, bool defaultValue)
        {
            var value = await GetValueAsync(key);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        public async Task<bool> SetValueAsync(string key, string value)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                UPDATE AppConfiguration
                SET [Value] = @Value, UpdatedAt = GETUTCDATE()
                WHERE [Key] = @Key
            ", connection);

            cmd.Parameters.AddWithValue("@Key", key);
            cmd.Parameters.AddWithValue("@Value", value ?? string.Empty);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            // Clear cache for this key
            _cache.Remove(key);

            return rowsAffected > 0;
        }

        /// <summary>
        /// Creates a new configuration entry
        /// </summary>
        public async Task<int> CreateAsync(AppConfiguration config)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO AppConfiguration ([Key], [Value], Description, DataType, Category)
                VALUES (@Key, @Value, @Description, @DataType, @Category);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", connection);

            cmd.Parameters.AddWithValue("@Key", config.Key);
            cmd.Parameters.AddWithValue("@Value", (object?)config.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)config.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DataType", config.DataType);
            cmd.Parameters.AddWithValue("@Category", config.Category);

            var result = await cmd.ExecuteScalarAsync();

            // Clear cache
            _cache.Clear();

            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Gets all configurations
        /// </summary>
        public async Task<List<AppConfiguration>> GetAllAsync(string? category = null)
        {
            var configs = new List<AppConfiguration>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM AppConfiguration";
            if (!string.IsNullOrEmpty(category))
                query += " WHERE Category = @Category";
            query += " ORDER BY Category, [Key]";

            await using var cmd = new SqlCommand(query, connection);

            if (!string.IsNullOrEmpty(category))
                cmd.Parameters.AddWithValue("@Category", category);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                configs.Add(MapFromReader(reader));
            }

            return configs;
        }

        /// <summary>
        /// Gets all scraping configurations
        /// </summary>
        public async Task<Dictionary<string, string>> GetScrapingConfigAsync()
        {
            var config = new Dictionary<string, string>();

            config["Count"] = await GetValueAsync(ConfigurationKeys.ScrapingCount) ?? Constants.DefaultScrapingCount.ToString();
            config["RefreshInterval"] = await GetValueAsync(ConfigurationKeys.ScrapingRefreshInterval) ?? Constants.DefaultRefreshInterval.ToString();
            config["SitemapUrl"] = await GetValueAsync(ConfigurationKeys.ScrapingSitemapUrl) ?? Constants.DefaultSitemapUrl;
            config["CardCssClass"] = await GetValueAsync(ConfigurationKeys.ScrapingCardCssClass) ?? Constants.DefaultCardCssClass;
            config["ContentCssClass"] = await GetValueAsync(ConfigurationKeys.ScrapingContentCssClass) ?? Constants.DefaultContentCssClass;
            config["RetryCount"] = await GetValueAsync(ConfigurationKeys.ScrapingRetryCount) ?? Constants.DefaultRetryCount.ToString();
            config["RetryDelay"] = await GetValueAsync(ConfigurationKeys.ScrapingRetryDelay) ?? Constants.DefaultRetryDelay.ToString();
            config["Timeout"] = await GetValueAsync(ConfigurationKeys.ScrapingTimeout) ?? Constants.DefaultTimeout.ToString();

            return config;
        }

        /// <summary>
        /// Clears the configuration cache
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            _lastCacheUpdate = DateTime.MinValue;
        }

        /// <summary>
        /// Maps a SqlDataReader row to an AppConfiguration object
        /// </summary>
        private AppConfiguration MapFromReader(SqlDataReader reader)
        {
            return new AppConfiguration
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                Key = reader.GetString(reader.GetOrdinal("Key")),
                Value = reader.IsDBNull(reader.GetOrdinal("Value")) ? null : reader.GetString(reader.GetOrdinal("Value")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}
