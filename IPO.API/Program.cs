using IPO.API.Data;
using IPO.API.Services;
using IPO.API.Hubs;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/ipo-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IPO Data Collection API",
        Version = "v1",
        Description = "API for IPO web scraping and data management"
    });
});

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register Data Layer
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IPORepository>();
builder.Services.AddSingleton<IPOPremiumMappingRepository>();
builder.Services.AddSingleton<ConfigurationRepository>();
builder.Services.AddSingleton<ScrapingLogRepository>();

// Register Services
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<SitemapParserService>();
builder.Services.AddSingleton<IPOScraperService>();
builder.Services.AddSingleton<ScrapingOrchestratorService>();

// Register Background Service for scheduled scraping
builder.Services.AddHostedService<ScrapingBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHub<ScrapingHub>("/scrapingHub");

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    try
    {
        await dbInitializer.InitializeDatabaseAsync();
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize database");
    }
}

Log.Information("IPO API starting...");
app.Run();
