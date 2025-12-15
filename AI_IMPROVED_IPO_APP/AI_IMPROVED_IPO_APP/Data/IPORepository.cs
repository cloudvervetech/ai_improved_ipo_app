using AI_IMPROVED_IPO_APP.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AI_IMPROVED_IPO_APP.Data
{
    /// <summary>
    /// Repository for IPO data operations
    /// </summary>
    public class IPORepository
    {
        private readonly ILogger<IPORepository> _logger;
        private readonly string _connectionString;

        public IPORepository(ILogger<IPORepository> logger, string? connectionString = null)
        {
            _logger = logger;
            _connectionString = connectionString ?? Constants.MSSQLConnectionString;
        }

        /// <summary>
        /// Gets all IPOs with optional filtering
        /// </summary>
        public async Task<List<IPO>> GetAllAsync(string? category = null, string? status = null, bool? isActive = null)
        {
            var ipos = new List<IPO>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = new StringBuilder("SELECT * FROM IPO WHERE 1=1");

            if (!string.IsNullOrEmpty(category))
                query.Append(" AND Category = @Category");

            if (!string.IsNullOrEmpty(status))
                query.Append(" AND Status = @Status");

            if (isActive.HasValue)
                query.Append(" AND IsActive = @IsActive");

            query.Append(" ORDER BY CreatedAt DESC");

            await using var cmd = new SqlCommand(query.ToString(), connection);

            if (!string.IsNullOrEmpty(category))
                cmd.Parameters.AddWithValue("@Category", category);

            if (!string.IsNullOrEmpty(status))
                cmd.Parameters.AddWithValue("@Status", status);

            if (isActive.HasValue)
                cmd.Parameters.AddWithValue("@IsActive", isActive.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ipos.Add(MapIPOFromReader(reader));
            }

            return ipos;
        }

        /// <summary>
        /// Gets an IPO by ID
        /// </summary>
        public async Task<IPO?> GetByIdAsync(int id)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand("SELECT * FROM IPO WHERE ID = @ID", connection);
            cmd.Parameters.AddWithValue("@ID", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapIPOFromReader(reader);
            }

            return null;
        }

        /// <summary>
        /// Searches IPOs by name
        /// </summary>
        public async Task<List<IPO>> SearchByNameAsync(string searchTerm)
        {
            var ipos = new List<IPO>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT * FROM IPO WHERE Name LIKE @SearchTerm ORDER BY CreatedAt DESC",
                connection);
            cmd.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ipos.Add(MapIPOFromReader(reader));
            }

            return ipos;
        }

        /// <summary>
        /// Creates a new IPO
        /// </summary>
        public async Task<int> CreateAsync(IPO ipo)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO IPO (Name, CardHtml, ContentHtml, Category, ScrapedAt, Status,
                                IssuePrice, LotSize, OpenDate, CloseDate, IssueSize, IsActive)
                VALUES (@Name, @CardHtml, @ContentHtml, @Category, @ScrapedAt, @Status,
                        @IssuePrice, @LotSize, @OpenDate, @CloseDate, @IssueSize, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", connection);

            cmd.Parameters.AddWithValue("@Name", ipo.Name);
            cmd.Parameters.AddWithValue("@CardHtml", (object?)ipo.CardHtml ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContentHtml", (object?)ipo.ContentHtml ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Category", ipo.Category);
            cmd.Parameters.AddWithValue("@ScrapedAt", ipo.ScrapedAt);
            cmd.Parameters.AddWithValue("@Status", (object?)ipo.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IssuePrice", (object?)ipo.IssuePrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LotSize", (object?)ipo.LotSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OpenDate", (object?)ipo.OpenDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CloseDate", (object?)ipo.CloseDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IssueSize", (object?)ipo.IssueSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", ipo.IsActive);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates an existing IPO
        /// </summary>
        public async Task<bool> UpdateAsync(IPO ipo)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                UPDATE IPO
                SET Name = @Name,
                    CardHtml = @CardHtml,
                    ContentHtml = @ContentHtml,
                    Category = @Category,
                    ScrapedAt = @ScrapedAt,
                    UpdatedAt = GETUTCDATE(),
                    Status = @Status,
                    IssuePrice = @IssuePrice,
                    LotSize = @LotSize,
                    OpenDate = @OpenDate,
                    CloseDate = @CloseDate,
                    IssueSize = @IssueSize,
                    IsActive = @IsActive
                WHERE ID = @ID
            ", connection);

            cmd.Parameters.AddWithValue("@ID", ipo.ID);
            cmd.Parameters.AddWithValue("@Name", ipo.Name);
            cmd.Parameters.AddWithValue("@CardHtml", (object?)ipo.CardHtml ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContentHtml", (object?)ipo.ContentHtml ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Category", ipo.Category);
            cmd.Parameters.AddWithValue("@ScrapedAt", ipo.ScrapedAt);
            cmd.Parameters.AddWithValue("@Status", (object?)ipo.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IssuePrice", (object?)ipo.IssuePrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LotSize", (object?)ipo.LotSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OpenDate", (object?)ipo.OpenDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CloseDate", (object?)ipo.CloseDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IssueSize", (object?)ipo.IssueSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", ipo.IsActive);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Deletes an IPO
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand("DELETE FROM IPO WHERE ID = @ID", connection);
            cmd.Parameters.AddWithValue("@ID", id);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Gets IPO statistics by category
        /// </summary>
        public async Task<Dictionary<string, int>> GetCategoryStatsAsync()
        {
            var stats = new Dictionary<string, int>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                SELECT Category, COUNT(*) as Count
                FROM IPO
                WHERE IsActive = 1
                GROUP BY Category
            ", connection);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats[reader.GetString(0)] = reader.GetInt32(1);
            }

            return stats;
        }

        /// <summary>
        /// Maps a SqlDataReader row to an IPO object
        /// </summary>
        private IPO MapIPOFromReader(SqlDataReader reader)
        {
            return new IPO
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                CardHtml = reader.IsDBNull(reader.GetOrdinal("CardHtml")) ? null : reader.GetString(reader.GetOrdinal("CardHtml")),
                ContentHtml = reader.IsDBNull(reader.GetOrdinal("ContentHtml")) ? null : reader.GetString(reader.GetOrdinal("ContentHtml")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                ScrapedAt = reader.GetDateTime(reader.GetOrdinal("ScrapedAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
                IssuePrice = reader.IsDBNull(reader.GetOrdinal("IssuePrice")) ? null : reader.GetString(reader.GetOrdinal("IssuePrice")),
                LotSize = reader.IsDBNull(reader.GetOrdinal("LotSize")) ? null : reader.GetInt32(reader.GetOrdinal("LotSize")),
                OpenDate = reader.IsDBNull(reader.GetOrdinal("OpenDate")) ? null : reader.GetDateTime(reader.GetOrdinal("OpenDate")),
                CloseDate = reader.IsDBNull(reader.GetOrdinal("CloseDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CloseDate")),
                IssueSize = reader.IsDBNull(reader.GetOrdinal("IssueSize")) ? null : reader.GetString(reader.GetOrdinal("IssueSize")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };
        }
    }
}
