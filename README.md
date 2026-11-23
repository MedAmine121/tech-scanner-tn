# TechScannerTN

Full-stack Tunisian tech hardware price comparison and scraping platform built with **ASP.NET Core 8**, **Entity Framework Core 8 (SQL Server)**, **Redis**, and **Angular 21**. It automatically scrapes product catalogs and prices from **MyTek**, **TunisiaNet**, and **SpaceNet**, tracks historical price trends, and exposes comparison and analytics APIs.

## Prerequisites

- [Docker](https://www.docker.com/) & Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js & npm](https://nodejs.org/) (for `TechScannerTN.Angular`)

## 1. Start Infrastructure (SQL Server + Redis)

The repository includes a [`docker-compose.yml`](docker-compose.yml) defining containerized **SQL Server 2022** and **Redis 7** instances with persistent volumes and health checks.

```bash
# Start SQL Server (port 1433) and Redis (port 6379) in the background
docker compose up -d
```

Verify both containers are running and healthy:

```bash
docker compose ps
docker exec techscanner-redis redis-cli ping
# Expected output: PONG
```

### Optional: Custom SQL Server SA Password

By default, `MSSQL_SA_PASSWORD` is set to `TechScanner@2026Password!`. To override it:

```bash
MSSQL_SA_PASSWORD="YourStrongPassword!" docker compose up -d
```

Make sure to update `ConnectionStrings:DefaultConnection` in `TechScannerTN/appsettings.json` (or `appsettings.Development.json`) to match.

### Stop or Reset Containers

```bash
# Stop containers while preserving data volumes
docker compose down

# Stop containers and delete persisted database and Redis volumes
docker compose down -v
```

## 2. Configuration

Backend connection strings are configured in `TechScannerTN/appsettings.json` (and `TechScannerTN/appsettings.Development.json`, which is gitignored for local overrides):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TechScannerTN;User Id=sa;Password=TechScanner@2026Password!;TrustServerCertificate=True;MultipleActiveResultSets=True;",
    "Redis": "localhost:6379,abortConnect=false,connectTimeout=2000"
  },
  "Redis": {
    "InstanceName": "TechScannerTN:"
  }
}
```

- **Fail-open caching**: If Redis is temporarily unavailable, `RedisCacheService` logs a warning and falls back directly to SQL Server without failing API requests.
- **In-memory fallback**: If `ConnectionStrings:Redis` is omitted or empty, `Program.cs` falls back to `AddDistributedMemoryCache()`.

## 3. Redis Caching Behavior

Read-heavy endpoints are cached via `ICacheService` / `RedisCacheService` with scope-based version-token invalidation:

| Area | Scope | Cached Data | TTL |
| :--- | :--- | :--- | :--- |
| `ProductCatalogService` | `catalog` | Filtered product listings (`GetProductsAsync`) | 10 min |
| `ProductCatalogService` | `catalog` | Product comparison details (`GetProductDetailsAsync`, non-null only) | 15 min |
| `ProductCatalogService` | `catalog` | Product price history (`GetPriceHistoryAsync`) | 30 min |
| `ProductCatalogService` | `catalog` | Price drops (`GetPriceDropsAsync`) | 15 min |
| `ProductCatalogService` | `catalog` | Market overview analytics (`GetMarketOverviewAsync`) | 15 min |
| `CategoriesController` | `categories` | Retailer & unified category endpoints | 60 min |

Cache invalidation runs automatically inside `ProductIngestionService`:
- `catalog` scope is invalidated when a new listing is added, a product's price or stock status changes, or a scrape session completes.
- `categories` scope is invalidated when retailer categories are updated.

## 4. Run the Application

### Backend API (.NET 8)

Entity Framework Core migrations are applied automatically on startup (`context.Database.Migrate()`).

```bash
# HTTP profile (http://localhost:5108)
dotnet run --project TechScannerTN

# HTTPS profile (https://localhost:7245;http://localhost:5108)
dotnet run --project TechScannerTN --launch-profile https
```

Swagger UI (Development environment):
- `http://localhost:5108/swagger`
- `https://localhost:7245/swagger`

### Run Unit Tests

```bash
dotnet test TechScannerTN.Tests/TechScannerTN.Tests.csproj
```

### Frontend (Angular 21)

```bash
cd TechScannerTN.Angular
npm install
npm start
```

The Angular dev server starts via `ng serve` (typically at `http://localhost:4200`).
