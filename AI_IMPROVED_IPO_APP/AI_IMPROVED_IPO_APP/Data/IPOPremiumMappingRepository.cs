using AI_IMPROVED_IPO_APP.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AI_IMPROVED_IPO_APP.Data
{
    /// <summary>
    /// Repository for IPOPremiumMapping operations
    /// </summary>
    public class IPOPremiumMappingRepository
    {
        private readonly ILogger<IPOPremiumMappingRepository> _logger;
        private readonly string _connectionString;

        public IPOPremiumMappingRepository(ILogger<IPOPremiumMappingRepository> logger, string? connectionString = null)
        {
            _logger = logger;
            _connectionString = connectionString ?? Constants.MSSQLConnectionString;
        }

        /// <summary>
        /// Creates a new mapping
        /// </summary>
        public async Task<int> CreateAsync(IPOPremiumMapping mapping)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO IPOPremiumMapping (IPOID, IPOPremiumID, Slug, SourceUrl)
                VALUES (@IPOID, @IPOPremiumID, @Slug, @SourceUrl);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", connection);

            cmd.Parameters.AddWithValue("@IPOID", mapping.IPOID);
            cmd.Parameters.AddWithValue("@IPOPremiumID", mapping.IPOPremiumID);
            cmd.Parameters.AddWithValue("@Slug", mapping.Slug);
            cmd.Parameters.AddWithValue("@SourceUrl", mapping.SourceUrl);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Gets mapping by IPOPremiumID
        /// </summary>
        public async Task<IPOPremiumMapping?> GetByIPOPremiumIDAsync(int ipoPremiumID)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT * FROM IPOPremiumMapping WHERE IPOPremiumID = @IPOPremiumID",
                connection);
            cmd.Parameters.AddWithValue("@IPOPremiumID", ipoPremiumID);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        /// <summary>
        /// Gets mapping by IPO ID
        /// </summary>
        public async Task<IPOPremiumMapping?> GetByIPOIDAsync(int ipoID)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT * FROM IPOPremiumMapping WHERE IPOID = @IPOID",
                connection);
            cmd.Parameters.AddWithValue("@IPOID", ipoID);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        /// <summary>
        /// Checks if IPOPremiumID already exists
        /// </summary>
        public async Task<bool> ExistsAsync(int ipoPremiumID)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM IPOPremiumMapping WHERE IPOPremiumID = @IPOPremiumID",
                connection);
            cmd.Parameters.AddWithValue("@IPOPremiumID", ipoPremiumID);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Gets all mappings
        /// </summary>
        public async Task<List<IPOPremiumMapping>> GetAllAsync()
        {
            var mappings = new List<IPOPremiumMapping>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand("SELECT * FROM IPOPremiumMapping ORDER BY CreatedAt DESC", connection);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                mappings.Add(MapFromReader(reader));
            }

            return mappings;
        }

        /// <summary>
        /// Maps a SqlDataReader row to an IPOPremiumMapping object
        /// </summary>
        private IPOPremiumMapping MapFromReader(SqlDataReader reader)
        {
            return new IPOPremiumMapping
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                IPOID = reader.GetInt32(reader.GetOrdinal("IPOID")),
                IPOPremiumID = reader.GetInt32(reader.GetOrdinal("IPOPremiumID")),
                Slug = reader.GetString(reader.GetOrdinal("Slug")),
                SourceUrl = reader.GetString(reader.GetOrdinal("SourceUrl")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
