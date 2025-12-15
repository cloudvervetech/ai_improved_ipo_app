namespace AI_IMPROVED_IPO_APP.Data
{
    public static class Constants
    {
        // SQLite Configuration (for local data)
        public const string DatabaseFilename = "AppSQLite.db3";

        public static string DatabasePath =>
            $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename)}";

        // MSSQL Configuration (for IPO data)
        // TODO: Update this connection string with your actual SQL Server details
        public static string MSSQLConnectionString =>
            "Server=localhost;Database=AIIPODB;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;";

        // Default Configuration Values
        public const int DefaultScrapingCount = 20;
        public const int DefaultRefreshInterval = 10; // seconds
        public const string DefaultSitemapUrl = "https://www.ipopremium.in/sitemap.xml";
        public const string DefaultCardCssClass = "card card-primary card-outline";
        public const string DefaultContentCssClass = "col-md-8 order-1";
        public const int DefaultRetryCount = 3;
        public const int DefaultRetryDelay = 2000; // milliseconds
        public const int DefaultTimeout = 30000; // milliseconds
    }
}