# IPO Data Collection API

ASP.NET Core Web API backend service for automated IPO web scraping and data management.

## Features

- ✅ **RESTful API** for IPO data management
- ✅ **Web Scraping** from ipopremium.in with retry logic
- ✅ **SignalR Hub** for real-time progress updates
- ✅ **Background Service** for scheduled scraping
- ✅ **Swagger/OpenAPI** documentation
- ✅ **MSSQL Database** integration
- ✅ **Logging** with Serilog

## API Endpoints

### Scraping Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/scraping/start` | Start scraping process |
| POST | `/api/scraping/stop` | Stop scraping process |
| GET | `/api/scraping/status` | Get current scraping status |
| GET | `/api/scraping/history?count=10` | Get scraping history |
| GET | `/api/scraping/batch/{batchId}/logs` | Get logs for specific batch |

### IPO Data

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/ipo` | Get all IPOs (with optional filters) |
| GET | `/api/ipo/{id}` | Get specific IPO by ID |
| GET | `/api/ipo/stats` | Get IPO statistics |
| DELETE | `/api/ipo/{id}` | Delete an IPO |

**Query Parameters for `/api/ipo`:**
- `category` - Filter by SME or Mainboard
- `status` - Filter by status
- `isActive` - Filter by active status (true/false)
- `search` - Search by company name

### Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/configuration` | Get all configurations |
| GET | `/api/configuration/scraping` | Get scraping-specific config |
| GET | `/api/configuration/{key}` | Get specific config value |
| PUT | `/api/configuration/{key}` | Update config value |
| POST | `/api/configuration/cache/clear` | Clear config cache |

## SignalR Hub

**Hub URL:** `/scrapingHub`

### Client Methods (Receive from Server)

```javascript
// Progress updates
connection.on("ReceiveProgress", (data) => {
    // data: { current, total, percentage, currentIPO, status, timestamp }
});

// Status updates
connection.on("ReceiveStatusUpdate", (data) => {
    // data: { message, status, timestamp }
});

// Scraping complete
connection.on("ReceiveScrapingComplete", (data) => {
    // data: { successCount, failedCount, skippedCount, durationMs, timestamp }
});
```

## Configuration

### Database Connection

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=AIIPODB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

### Scraping Settings

```json
{
  "ScrapingSettings": {
    "DefaultCount": 20,
    "DefaultRefreshInterval": 10,
    "AutoScrapingEnabled": false
  }
}
```

## Running the API

### Development

```bash
cd IPO.API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### Production (Windows Server)

1. **Publish the application:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to IIS:**
   - Copy `publish` folder to server
   - Create IIS application
   - Configure application pool (.NET Core)
   - Set connection string in `appsettings.json`

3. **Or run as Windows Service:**
   ```bash
   # Install as Windows Service
   sc.exe create "IPO.API" binPath="C:\path\to\IPO.API.exe"
   sc.exe start "IPO.API"
   ```

## Database Initialization

The API automatically initializes the database on startup. Tables will be created if they don't exist.

Manual initialization via API:
```bash
# The database is initialized automatically
# No manual steps required
```

## Logging

Logs are written to:
- **Console** (stdout)
- **File** `logs/ipo-api-YYYYMMDD.log` (rotating daily)

## Background Scraping

To enable automatic scheduled scraping:

```sql
INSERT INTO AppConfiguration ([Key], [Value], Description, DataType, Category)
VALUES ('Scraping.AutoEnabled', 'true', 'Enable automatic scheduled scraping', 'bool', 'Scraping');
```

Or via API:
```bash
curl -X PUT https://localhost:5001/api/configuration/Scraping.AutoEnabled \
  -H "Content-Type: application/json" \
  -d '{"value": "true"}'
```

## Architecture

```
IPO.API/
├── Controllers/
│   ├── ScrapingController.cs    # Scraping operations
│   ├── IPOController.cs          # IPO data management
│   └── ConfigurationController.cs # Configuration management
├── Services/
│   ├── SitemapParserService.cs   # Sitemap parsing
│   ├── IPOScraperService.cs      # HTML scraping
│   ├── ScrapingOrchestratorService.cs # Workflow coordination
│   └── ScrapingBackgroundService.cs # Scheduled scraping
├── Data/
│   ├── DatabaseInitializer.cs    # Schema creation
│   ├── IPORepository.cs          # IPO data access
│   ├── IPOPremiumMappingRepository.cs
│   ├── ConfigurationRepository.cs
│   └── ScrapingLogRepository.cs
├── Models/
│   ├── IPO.cs
│   ├── IPOPremiumMapping.cs
│   ├── AppConfiguration.cs
│   └── ScrapingLog.cs
├── Hubs/
│   └── ScrapingHub.cs            # SignalR hub
└── Program.cs                     # Application entry point
```

## Dependencies

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
<PackageReference Include="HtmlAgilityPack" Version="1.11.71" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

## Testing

### Test Endpoints with cURL

```bash
# Start scraping
curl -X POST https://localhost:5001/api/scraping/start

# Get status
curl https://localhost:5001/api/scraping/status

# Get all IPOs
curl https://localhost:5001/api/ipo

# Search IPOs
curl "https://localhost:5001/api/ipo?search=technocrats"

# Get IPO statistics
curl https://localhost:5001/api/ipo/stats

# Get scraping configuration
curl https://localhost:5001/api/configuration/scraping
```

### Test SignalR Connection (JavaScript)

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5001/scrapingHub")
    .build();

connection.on("ReceiveProgress", (data) => {
    console.log("Progress:", data);
});

await connection.start();
```

## Security Considerations

1. **Authentication/Authorization**: Add JWT authentication for production
2. **CORS**: Configure specific origins in production (not AllowAll)
3. **API Rate Limiting**: Implement rate limiting for public endpoints
4. **SQL Injection**: All queries use parameterized commands
5. **HTTPS**: Always use HTTPS in production

## Troubleshooting

### Database Connection Issues
```bash
# Test connection from API
curl https://localhost:5001/api/scraping/status
```

### Scraping Failures
```bash
# Check logs
tail -f logs/ipo-api-*.log

# Get batch details
curl https://localhost:5001/api/scraping/history
```

### SignalR Connection Issues
- Verify WebSocket support is enabled
- Check firewall rules
- Enable detailed SignalR logging

## Support

For issues and feature requests, please check the main project documentation.

---

**Version:** 1.0
**Last Updated:** December 2025
**License:** Proprietary
