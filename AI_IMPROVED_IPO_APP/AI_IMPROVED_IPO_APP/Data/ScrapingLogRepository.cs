using AI_IMPROVED_IPO_APP.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AI_IMPROVED_IPO_APP.Data
{
    /// <summary>
    /// Repository for ScrapingLog operations
    /// </summary>
    public class ScrapingLogRepository
    {
        private readonly ILogger<ScrapingLogRepository> _logger;
        private readonly string _connectionString;

        public ScrapingLogRepository(ILogger<ScrapingLogRepository> logger, string? connectionString = null)
        {
            _logger = logger;
            _connectionString = connectionString ?? Constants.MSSQLConnectionString;
        }

        /// <summary>
        /// Creates a new scraping log entry
        /// </summary>
        public async Task<int> CreateAsync(ScrapingLog log)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO ScrapingLog (BatchID, IPOPremiumID, Url, Status, CurrentStep,
                                        ErrorMessage, StackTrace, RetryCount, StartedAt,
                                        CompletedAt, DurationMs, IPOID)
                VALUES (@BatchID, @IPOPremiumID, @Url, @Status, @CurrentStep,
                        @ErrorMessage, @StackTrace, @RetryCount, @StartedAt,
                        @CompletedAt, @DurationMs, @IPOID);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ", connection);

            cmd.Parameters.AddWithValue("@BatchID", log.BatchID);
            cmd.Parameters.AddWithValue("@IPOPremiumID", (object?)log.IPOPremiumID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Url", (object?)log.Url ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (int)log.Status);
            cmd.Parameters.AddWithValue("@CurrentStep", (object?)log.CurrentStep ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ErrorMessage", (object?)log.ErrorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StackTrace", (object?)log.StackTrace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RetryCount", log.RetryCount);
            cmd.Parameters.AddWithValue("@StartedAt", (object?)log.StartedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CompletedAt", (object?)log.CompletedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DurationMs", (object?)log.DurationMs ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IPOID", (object?)log.IPOID ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates an existing scraping log entry
        /// </summary>
        public async Task<bool> UpdateAsync(ScrapingLog log)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                UPDATE ScrapingLog
                SET Status = @Status,
                    CurrentStep = @CurrentStep,
                    ErrorMessage = @ErrorMessage,
                    StackTrace = @StackTrace,
                    RetryCount = @RetryCount,
                    StartedAt = @StartedAt,
                    CompletedAt = @CompletedAt,
                    DurationMs = @DurationMs,
                    IPOID = @IPOID
                WHERE ID = @ID
            ", connection);

            cmd.Parameters.AddWithValue("@ID", log.ID);
            cmd.Parameters.AddWithValue("@Status", (int)log.Status);
            cmd.Parameters.AddWithValue("@CurrentStep", (object?)log.CurrentStep ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ErrorMessage", (object?)log.ErrorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StackTrace", (object?)log.StackTrace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RetryCount", log.RetryCount);
            cmd.Parameters.AddWithValue("@StartedAt", (object?)log.StartedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CompletedAt", (object?)log.CompletedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DurationMs", (object?)log.DurationMs ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IPOID", (object?)log.IPOID ?? DBNull.Value);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Gets logs by batch ID
        /// </summary>
        public async Task<List<ScrapingLog>> GetByBatchAsync(Guid batchID)
        {
            var logs = new List<ScrapingLog>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT * FROM ScrapingLog WHERE BatchID = @BatchID ORDER BY CreatedAt",
                connection);
            cmd.Parameters.AddWithValue("@BatchID", batchID);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(MapFromReader(reader));
            }

            return logs;
        }

        /// <summary>
        /// Gets batch summary statistics
        /// </summary>
        public async Task<ScrapingBatchSummary?> GetBatchSummaryAsync(Guid batchID)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand(@"
                SELECT
                    COUNT(*) as TotalCount,
                    SUM(CASE WHEN Status = @Completed THEN 1 ELSE 0 END) as CompletedCount,
                    SUM(CASE WHEN Status = @Failed THEN 1 ELSE 0 END) as FailedCount,
                    SUM(CASE WHEN Status = @Pending THEN 1 ELSE 0 END) as PendingCount,
                    SUM(CASE WHEN Status = @InProgress THEN 1 ELSE 0 END) as InProgressCount,
                    MIN(StartedAt) as StartedAt,
                    MAX(CompletedAt) as CompletedAt
                FROM ScrapingLog
                WHERE BatchID = @BatchID
            ", connection);

            cmd.Parameters.AddWithValue("@BatchID", batchID);
            cmd.Parameters.AddWithValue("@Completed", (int)ScrapingStatus.Completed);
            cmd.Parameters.AddWithValue("@Failed", (int)ScrapingStatus.Failed);
            cmd.Parameters.AddWithValue("@Pending", (int)ScrapingStatus.Pending);
            cmd.Parameters.AddWithValue("@InProgress", (int)ScrapingStatus.InProgress);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var totalCount = reader.GetInt32(0);
                if (totalCount == 0) return null;

                return new ScrapingBatchSummary
                {
                    BatchID = batchID,
                    TotalCount = totalCount,
                    CompletedCount = reader.GetInt32(1),
                    FailedCount = reader.GetInt32(2),
                    PendingCount = reader.GetInt32(3),
                    InProgressCount = reader.GetInt32(4),
                    StartedAt = reader.IsDBNull(5) ? DateTime.UtcNow : reader.GetDateTime(5),
                    CompletedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
                };
            }

            return null;
        }

        /// <summary>
        /// Gets recent batches
        /// </summary>
        public async Task<List<Guid>> GetRecentBatchesAsync(int count = 10)
        {
            var batches = new List<Guid>();
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new SqlCommand($@"
                SELECT DISTINCT TOP {count} BatchID
                FROM ScrapingLog
                ORDER BY CreatedAt DESC
            ", connection);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                batches.Add(reader.GetGuid(0));
            }

            return batches;
        }

        /// <summary>
        /// Gets the latest batch summary
        /// </summary>
        public async Task<ScrapingBatchSummary?> GetLatestBatchSummaryAsync()
        {
            var batches = await GetRecentBatchesAsync(1);
            if (batches.Count == 0) return null;

            return await GetBatchSummaryAsync(batches[0]);
        }

        /// <summary>
        /// Maps a SqlDataReader row to a ScrapingLog object
        /// </summary>
        private ScrapingLog MapFromReader(SqlDataReader reader)
        {
            return new ScrapingLog
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                BatchID = reader.GetGuid(reader.GetOrdinal("BatchID")),
                IPOPremiumID = reader.IsDBNull(reader.GetOrdinal("IPOPremiumID")) ? null : reader.GetInt32(reader.GetOrdinal("IPOPremiumID")),
                Url = reader.IsDBNull(reader.GetOrdinal("Url")) ? null : reader.GetString(reader.GetOrdinal("Url")),
                Status = (ScrapingStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                CurrentStep = reader.IsDBNull(reader.GetOrdinal("CurrentStep")) ? null : reader.GetString(reader.GetOrdinal("CurrentStep")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                StackTrace = reader.IsDBNull(reader.GetOrdinal("StackTrace")) ? null : reader.GetString(reader.GetOrdinal("StackTrace")),
                RetryCount = reader.GetInt32(reader.GetOrdinal("RetryCount")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                DurationMs = reader.IsDBNull(reader.GetOrdinal("DurationMs")) ? null : reader.GetInt64(reader.GetOrdinal("DurationMs")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IPOID = reader.IsDBNull(reader.GetOrdinal("IPOID")) ? null : reader.GetInt32(reader.GetOrdinal("IPOID"))
            };
        }
    }
}
