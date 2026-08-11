# Web Scraping Service Implementation Summary

## Overview
Added a comprehensive **WebScraperHostedService** that automatically scrapes internet provider data from three major Tunisian ISPs and stores results in SQL Server.

## Files Created

### 1. **Data Models** (TechScannerTN.Models)
- `InternetProvider.cs` - Represents an ISP with properties for Name, Website, and related Plans
- `Plan.cs` - Represents an internet plan with Speed, Price, DataLimit, and ScrapedAt timestamp

### 2. **Database Layer** (TechScannerTN.DAL)
- `TechScannerContext.cs` - Entity Framework Core DbContext with:
  - DbSet<InternetProvider>
  - DbSet<Plan>
  - Proper model configuration with relationships and indexes
- `Migrations/20260811145314_InitialCreate.cs` - Initial database migration creating InternetProviders and Plans tables

### 3. **Services** (TechScannerTN.Services)
- `IWebScraper.cs` - Interface for web scrapers
- `MyTekScraper.cs` - Scraper for MyTek (https://www.mytek.tn)
- `TunisiaNetScraper.cs` - Scraper for TunisiaNet (https://www.tunisianet.tn)
- `SpaceNetScraper.cs` - Scraper for SpaceNet (https://www.spacenet.tn)
- `WebScraperHostedService.cs` - Main background service that:
  - Runs scrapers every 6 hours (configurable)
  - Manages database operations
  - Provides comprehensive logging
  - Handles errors gracefully

### 4. **API Endpoints** (TechScannerTN)
- `Endpoints/InternetProviderEndpoints.cs` - REST API endpoints:
  - `GET /api/providers` - Get all providers
  - `GET /api/providers/{id}` - Get provider with plans
  - `GET /api/providers/{id}/plans` - Get provider's plans

### 5. **Configuration**
- `appsettings.json` - SQL Server connection string and logging configuration
- `Program.cs` - Service registration and middleware setup
- Updated project files with required NuGet packages:
  - HtmlAgilityPack (HTML parsing)
  - EntityFrameworkCore & EntityFrameworkCore.SqlServer
  - Hosting abstractions for HostedService

### 6. **Documentation**
- `SCRAPING_SERVICE_README.md` - Complete documentation covering:
  - Setup and configuration
  - API endpoint usage
  - Data models
  - Customizing scrapers
  - Troubleshooting

## Key Features

✅ **Automatic Scraping** - Runs on a configurable interval (default: 6 hours)
✅ **Database Persistence** - Stores all data in SQL Server using Entity Framework Core
✅ **Error Handling** - Comprehensive try-catch and logging
✅ **REST API** - Query scraped data via HTTP endpoints
✅ **Data Versioning** - Tracks when each plan was scraped
✅ **Async/Await** - Non-blocking operations throughout
✅ **Dependency Injection** - Properly registered services

## Database Schema

### InternetProviders Table
- Id (PK)
- Name (string)
- Website (string)
- CreatedAt (datetime)
- UpdatedAt (datetime)

### Plans Table
- Id (PK)
- InternetProviderId (FK)
- Name (string)
- Speed (double)
- SpeedUnit (string)
- Price (decimal)
- Currency (string)
- Description (string)
- DataLimit (string)
- ScrapedAt (datetime)
- CreatedAt (datetime)
- UpdatedAt (datetime)

## Usage

### 1. Configure Connection String
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TechScannerTN;Trusted_Connection=true;"
  }
}
```

### 2. Run Application
```bash
dotnet run --project TechScannerTN
```

The service will:
- Create database schema on startup
- Run initial scrape immediately
- Schedule periodic scrapes every 6 hours
- Listen for API requests

### 3. Query Results
```bash
# Get all providers
curl https://localhost:5001/api/providers

# Get specific provider
curl https://localhost:5001/api/providers/1

# Get provider's plans
curl https://localhost:5001/api/providers/1/plans
```

## Customization

### Change Scraping Interval
Edit `WebScraperHostedService.cs`:
```csharp
private readonly TimeSpan _scrapeInterval = TimeSpan.FromHours(6);
```

### Update CSS Selectors
Each scraper class has an `ExtractPlanFromNode` method where you can update selectors to match the website structure:
```csharp
var name = node.SelectSingleNode(".//h3")?.InnerText;
```

### Add New Provider
1. Create `NewProviderScraper.cs` implementing `IWebScraper`
2. Register in `Program.cs`: `builder.Services.AddScoped<IWebScraper, NewProviderScraper>();`

## Logging
Logs are configured in `appsettings.json`. For debugging, set:
```json
{
  "Logging": {
    "LogLevel": {
      "Hi_Trade.Services": "Debug"
    }
  }
}
```

## Testing
To verify the scraper works:
1. Build: `dotnet build`
2. Run: `dotnet run --project TechScannerTN`
3. Check logs for scraping activity
4. Query API endpoints to verify data

## Dependencies Added
- **HtmlAgilityPack** (1.11.61) - HTML parsing and DOM manipulation
- **Microsoft.EntityFrameworkCore** (8.0.8) - ORM
- **Microsoft.EntityFrameworkCore.SqlServer** (8.0.8) - SQL Server provider
- **Microsoft.EntityFrameworkCore.Design** (8.0.8) - Migration tools

## Next Steps
- Adjust CSS selectors if website structures change
- Monitor scraping logs for errors
- Consider implementing price history tracking
- Add notifications for significant price changes
- Expand to additional providers as needed
