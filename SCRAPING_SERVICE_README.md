# Tech Scanner TN - Web Scraping Service

## Overview

The Tech Scanner TN application includes a **WebScraperHostedService** that automatically scrapes internet provider data (plans, speeds, prices) from three major Tunisian ISPs:

- **MyTek** (https://www.mytek.tn)
- **TunisiaNet** (https://www.tunisianet.tn)
- **SpaceNet** (https://www.spacenet.tn)

The scraped data is automatically stored in a SQL Server database and can be accessed via REST API endpoints.

## Features

✅ **Automatic Scraping**: Scrapes every 6 hours by default (configurable)
✅ **Database Storage**: Stores scraped data in SQL Server with Entity Framework Core
✅ **REST API**: Endpoints to query providers and their internet plans
✅ **Error Handling**: Comprehensive logging and error recovery
✅ **Data Versioning**: Tracks when each plan was scraped

## Database Setup

### Connection String
Update `appsettings.json` with your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TechScannerTN;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### Database Initialization
The database schema is automatically created on application startup using Entity Framework Core migrations:

```csharp
// Automatic migration in Program.cs
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TechScannerContext>();
    context.Database.Migrate();
}
```

## Data Models

### InternetProvider
Represents an internet service provider:
- `Id`: Primary key
- `Name`: Provider name (MyTek, TunisiaNet, SpaceNet)
- `Website`: Provider website URL
- `CreatedAt`: Record creation timestamp
- `UpdatedAt`: Last update timestamp
- `Plans`: Collection of Plan records

### Plan
Represents an internet plan/offer:
- `Id`: Primary key
- `InternetProviderId`: Foreign key to InternetProvider
- `Name`: Plan name/identifier
- `Speed`: Internet speed (numeric)
- `SpeedUnit`: Unit of speed (default: "Mbps")
- `Price`: Plan price
- `Currency`: Currency code (default: "TND")
- `Description`: Plan description
- `DataLimit`: Data limit information (if applicable)
- `ScrapedAt`: Timestamp when this plan was last scraped
- `CreatedAt`: Record creation timestamp
- `UpdatedAt`: Last update timestamp

## API Endpoints

### 1. Get All Providers
```http
GET /api/providers
```

**Response**:
```json
{
  "count": 3,
  "providers": [
    {
      "id": 1,
      "name": "MyTek",
      "website": "https://www.mytek.tn",
      "planCount": 5,
      "lastUpdated": "2026-08-11T15:45:00Z",
      "createdAt": "2026-08-11T15:30:00Z"
    }
  ]
}
```

### 2. Get Provider by ID
```http
GET /api/providers/{id}
```

**Response**:
```json
{
  "id": 1,
  "name": "MyTek",
  "website": "https://www.mytek.tn",
  "planCount": 5,
  "lastUpdated": "2026-08-11T15:45:00Z",
  "createdAt": "2026-08-11T15:30:00Z",
  "plans": [
    {
      "id": 1,
      "name": "Pro Plan",
      "speed": 100,
      "speedUnit": "Mbps",
      "price": 49.99,
      "currency": "TND",
      "description": "High-speed internet plan",
      "dataLimit": "Unlimited",
      "scrapedAt": "2026-08-11T15:45:00Z"
    }
  ]
}
```

### 3. Get Provider Plans
```http
GET /api/providers/{id}/plans
```

**Response**:
```json
{
  "count": 5,
  "plans": [
    {
      "id": 1,
      "name": "Starter Plan",
      "speed": 10,
      "speedUnit": "Mbps",
      "price": 9.99,
      "currency": "TND",
      "description": "Entry-level plan",
      "dataLimit": "10GB",
      "scrapedAt": "2026-08-11T15:45:00Z"
    }
  ]
}
```

## Service Configuration

### Scraping Interval
Modify the scraping interval in `WebScraperHostedService.cs`:

```csharp
private readonly TimeSpan _scrapeInterval = TimeSpan.FromHours(6); // Change interval here
```

### Logging
Configure logging levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Hi_Trade.Services": "Debug"
    }
  }
}
```

## Customizing Scrapers

Each scraper implements the `IWebScraper` interface:

```csharp
public interface IWebScraper
{
    Task<List<Plan>> ScrapeAsync();
    string ProviderName { get; }
}
```

To customize scraping logic for a provider:

1. Edit the corresponding scraper class (`MyTekScraper.cs`, `TunisiaNetScraper.cs`, or `SpaceNetScraper.cs`)
2. Update CSS selectors in the `ExtractPlanFromNode` method to match current website structure
3. Adjust price and speed parsing regex patterns if needed

### Example: Customizing CSS Selectors
```csharp
var plan = new Plan
{
    Name = node.SelectSingleNode(".//h3 | .//h2 | .//div[@class='plan-name']")?.InnerText?.Trim(),
    Speed = ParseSpeed(node.SelectSingleNode(".//div[@class='speed']")?.InnerText),
    Price = ParsePrice(node.SelectSingleNode(".//div[@class='price']")?.InnerText),
    // ... other properties
};
```

## Troubleshooting

### Migration Issues
If migrations fail, ensure EF Core tools are installed:
```bash
dotnet tool install --global dotnet-ef
```

### Connection String Issues
Verify SQL Server is running and connection string is correct:
```bash
sqlcmd -S SERVER_NAME -d TechScannerTN -Q "SELECT 1"
```

### Scraping Failures
Check logs for specific provider failures:
- Verify website URLs are accessible
- Ensure HTML structure hasn't changed (CSS selectors may need updating)
- Check network connectivity and firewall rules

### No Plans Found
If no plans are found after scraping:
1. Verify the website is accessible in a browser
2. Update CSS selectors in the scraper to match current HTML
3. Check logs for specific parsing errors

## Development

### Building
```bash
dotnet build
```

### Running
```bash
dotnet run --project TechScannerTN
```

### Testing
```bash
dotnet test
```

### Creating New Migrations
```bash
dotnet ef migrations add MigrationName -p TechScannerTN.DAL -s TechScannerTN
```

## Performance Considerations

- Scraping runs every 6 hours by default
- Old plans are removed and replaced during each scrape cycle
- Database indexes on `InternetProviderId`, `Name`, and `ScrapedAt` optimize queries
- All scraping is performed asynchronously without blocking the API

## Future Enhancements

- [ ] Schedule scraping to run at off-peak hours
- [ ] Add scraping result notifications
- [ ] Implement plan comparison features
- [ ] Add price history tracking
- [ ] Support additional providers
- [ ] Add proxy support for scraping
- [ ] Implement retry logic with exponential backoff
- [ ] Add webhook notifications on significant price changes
