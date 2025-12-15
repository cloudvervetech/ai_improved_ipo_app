using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace IPO.API.Data
{
    /// <summary>
    /// Handles database initialization and schema creation for MSSQL
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly ILogger<DatabaseInitializer> _logger;
        private readonly string _connectionString;

        public DatabaseInitializer(ILogger<DatabaseInitializer> logger, string? connectionString = null)
        {
            _logger = logger;
            _connectionString = connectionString ?? Constants.MSSQLConnectionString;
        }

        /// <summary>
        /// Initializes the database and creates all required tables
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                await CreateIPOTableAsync(connection);
                await CreateIPOPremiumMappingTableAsync(connection);
                await CreateAppConfigurationTableAsync(connection);
                await CreateScrapingLogTableAsync(connection);
                await SeedDefaultConfigurationAsync(connection);

                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        private async Task CreateIPOTableAsync(SqlConnection connection)
        {
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IPO')
                BEGIN
                    CREATE TABLE IPO (
                        ID INT PRIMARY KEY IDENTITY(1,1),
                        Name NVARCHAR(500) NOT NULL,
                        CardHtml NVARCHAR(MAX) NULL,
                        ContentHtml NVARCHAR(MAX) NULL,
                        Category NVARCHAR(50) NOT NULL DEFAULT 'Mainboard',
                        ScrapedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        Status NVARCHAR(100) NULL,
                        IssuePrice NVARCHAR(200) NULL,
                        LotSize INT NULL,
                        OpenDate DATETIME2 NULL,
                        CloseDate DATETIME2 NULL,
                        IssueSize NVARCHAR(200) NULL,
                        IsActive BIT NOT NULL DEFAULT 1
                    );

                    CREATE INDEX IX_IPO_Name ON IPO(Name);
                    CREATE INDEX IX_IPO_Category ON IPO(Category);
                    CREATE INDEX IX_IPO_Status ON IPO(Status);
                    CREATE INDEX IX_IPO_IsActive ON IPO(IsActive);
                    CREATE INDEX IX_IPO_CreatedAt ON IPO(CreatedAt DESC);
                END
            ";
            await createTableCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("IPO table created or already exists");
        }

        private async Task CreateIPOPremiumMappingTableAsync(SqlConnection connection)
        {
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IPOPremiumMapping')
                BEGIN
                    CREATE TABLE IPOPremiumMapping (
                        ID INT PRIMARY KEY IDENTITY(1,1),
                        IPOID INT NOT NULL,
                        IPOPremiumID INT NOT NULL,
                        Slug NVARCHAR(500) NOT NULL DEFAULT '',
                        SourceUrl NVARCHAR(1000) NOT NULL DEFAULT '',
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT FK_IPOPremiumMapping_IPO FOREIGN KEY (IPOID) REFERENCES IPO(ID) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX IX_IPOPremiumMapping_IPOPremiumID ON IPOPremiumMapping(IPOPremiumID);
                    CREATE INDEX IX_IPOPremiumMapping_IPOID ON IPOPremiumMapping(IPOID);
                END
            ";
            await createTableCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("IPOPremiumMapping table created or already exists");
        }

        private async Task CreateAppConfigurationTableAsync(SqlConnection connection)
        {
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppConfiguration')
                BEGIN
                    CREATE TABLE AppConfiguration (
                        ID INT PRIMARY KEY IDENTITY(1,1),
                        [Key] NVARCHAR(200) NOT NULL UNIQUE,
                        [Value] NVARCHAR(MAX) NULL,
                        Description NVARCHAR(1000) NULL,
                        DataType NVARCHAR(50) NOT NULL DEFAULT 'string',
                        Category NVARCHAR(200) NOT NULL DEFAULT 'General',
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                    );

                    CREATE UNIQUE INDEX IX_AppConfiguration_Key ON AppConfiguration([Key]);
                    CREATE INDEX IX_AppConfiguration_Category ON AppConfiguration(Category);
                END
            ";
            await createTableCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("AppConfiguration table created or already exists");
        }

        private async Task CreateScrapingLogTableAsync(SqlConnection connection)
        {
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScrapingLog')
                BEGIN
                    CREATE TABLE ScrapingLog (
                        ID INT PRIMARY KEY IDENTITY(1,1),
                        BatchID UNIQUEIDENTIFIER NOT NULL,
                        IPOPremiumID INT NULL,
                        Url NVARCHAR(1000) NULL,
                        Status INT NOT NULL DEFAULT 0,
                        CurrentStep NVARCHAR(200) NULL,
                        ErrorMessage NVARCHAR(MAX) NULL,
                        StackTrace NVARCHAR(MAX) NULL,
                        RetryCount INT NOT NULL DEFAULT 0,
                        StartedAt DATETIME2 NULL,
                        CompletedAt DATETIME2 NULL,
                        DurationMs BIGINT NULL,
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        IPOID INT NULL,
                        CONSTRAINT FK_ScrapingLog_IPO FOREIGN KEY (IPOID) REFERENCES IPO(ID)
                    );

                    CREATE INDEX IX_ScrapingLog_BatchID ON ScrapingLog(BatchID);
                    CREATE INDEX IX_ScrapingLog_Status ON ScrapingLog(Status);
                    CREATE INDEX IX_ScrapingLog_CreatedAt ON ScrapingLog(CreatedAt DESC);
                    CREATE INDEX IX_ScrapingLog_IPOPremiumID ON ScrapingLog(IPOPremiumID);
                END
            ";
            await createTableCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("ScrapingLog table created or already exists");
        }

        private async Task SeedDefaultConfigurationAsync(SqlConnection connection)
        {
            var seedCmd = connection.CreateCommand();
            seedCmd.CommandText = @"
                MERGE INTO AppConfiguration AS target
                USING (VALUES
                    ('Scraping.Count', '20', 'Number of IPOs to scrape from the latest ones', 'int', 'Scraping'),
                    ('Scraping.RefreshInterval', '10', 'Auto-refresh interval in seconds', 'int', 'Scraping'),
                    ('Scraping.SitemapUrl', 'https://www.ipopremium.in/sitemap.xml', 'URL of the IPO Premium sitemap', 'string', 'Scraping'),
                    ('Scraping.CardCssClass', 'card card-primary card-outline', 'CSS class for card element', 'string', 'Scraping'),
                    ('Scraping.ContentCssClass', 'col-md-8 order-1', 'CSS class for content element', 'string', 'Scraping'),
                    ('Scraping.RetryCount', '3', 'Number of retry attempts for failed scraping', 'int', 'Scraping'),
                    ('Scraping.RetryDelay', '2000', 'Delay between retries in milliseconds', 'int', 'Scraping'),
                    ('Scraping.Timeout', '30000', 'HTTP request timeout in milliseconds', 'int', 'Scraping')
                ) AS source ([Key], [Value], Description, DataType, Category)
                ON target.[Key] = source.[Key]
                WHEN NOT MATCHED THEN
                    INSERT ([Key], [Value], Description, DataType, Category, CreatedAt, UpdatedAt)
                    VALUES (source.[Key], source.[Value], source.Description, source.DataType, source.Category, GETUTCDATE(), GETUTCDATE());
            ";
            await seedCmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Default configuration seeded");
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed");
                return false;
            }
        }

        /// <summary>
        /// Drops all IPO-related tables (use with caution!)
        /// </summary>
        public async Task DropAllTablesAsync()
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var dropCmd = connection.CreateCommand();
                dropCmd.CommandText = @"
                    DROP TABLE IF EXISTS ScrapingLog;
                    DROP TABLE IF EXISTS IPOPremiumMapping;
                    DROP TABLE IF EXISTS AppConfiguration;
                    DROP TABLE IF EXISTS IPO;
                ";
                await dropCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("All tables dropped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping tables");
                throw;
            }
        }
    }
}
