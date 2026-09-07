using Microsoft.EntityFrameworkCore;
using Hi_Trade.Models;
using Hi_Trade.DAL.Entities;

namespace Hi_Trade.DAL;

public class TechScannerContext : DbContext
{
    public TechScannerContext(DbContextOptions<TechScannerContext> options) : base(options)
    {
    }

    public DbSet<InternetProvider> InternetProviders { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<User> Users { get; set; }

    // E-Commerce & Price Comparison System
    public DbSet<Retailer> Retailers { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<RetailerCategory> RetailerCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductListing> ProductListings { get; set; }
    public DbSet<PriceHistory> PriceHistories { get; set; }
    public DbSet<ScrapeSession> ScrapeSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Password).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(1000);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Internet Provider & Plans
        modelBuilder.Entity<InternetProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Website).IsRequired().HasMaxLength(200);
            entity.HasMany(e => e.Plans)
                .WithOne(p => p.InternetProvider)
                .HasForeignKey(p => p.InternetProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SpeedUnit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.InternetProviderId, e.Name });
            entity.HasIndex(e => e.ScrapedAt);
        });

        // Retailer
        modelBuilder.Entity<Retailer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.LogoUrl).HasMaxLength(1000);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.HasIndex(e => e.Code).IsUnique();

            // Seed initial 3 retailers
            entity.HasData(
                new Retailer
                {
                    Id = 1,
                    Code = "MYTEK",
                    Name = "MyTek",
                    BaseUrl = "https://www.mytek.tn/",
                    LogoUrl = "https://www.mytek.tn/media/logo/stores/1/mytek-logo.svg",
                    Currency = "TND",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Retailer
                {
                    Id = 2,
                    Code = "TUNISIANET",
                    Name = "TunisiaNet",
                    BaseUrl = "https://www.tunisianet.com.tn/",
                    LogoUrl = "https://www.tunisianet.com.tn/img/tunisianet-logo-1579277024.jpg",
                    Currency = "TND",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Retailer
                {
                    Id = 3,
                    Code = "SPACENET",
                    Name = "SpaceNet",
                    BaseUrl = "https://spacenet.tn/",
                    LogoUrl = "https://spacenet.tn/img/spacenet-tunisie-logo-1563276632.jpg",
                    Currency = "TND",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });

        // Brand
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(150);
            entity.Property(e => e.LogoUrl).HasMaxLength(1000);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        // Category (Unified hierarchical)
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RetailerCategory (Scraped store menus mapped to unified Category)
        modelBuilder.Entity<RetailerCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
            entity.Property(e => e.ParentCategoryName).HasMaxLength(250);

            entity.HasIndex(e => new { e.RetailerId, e.Url }).IsUnique();

            entity.HasOne(e => e.Retailer)
                .WithMany(r => r.RetailerCategories)
                .HasForeignKey(e => e.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.RetailerCategories)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Canonical Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.NormalizedSku).HasMaxLength(150);
            entity.Property(e => e.Ean).HasMaxLength(50);
            entity.Property(e => e.PrimaryImageUrl).HasMaxLength(1000);

            entity.Property(e => e.MinPrice).HasPrecision(18, 3);
            entity.Property(e => e.MaxPrice).HasPrecision(18, 3);
            entity.Property(e => e.HistoricalLowPrice).HasPrecision(18, 3);
            entity.Property(e => e.HistoricalHighPrice).HasPrecision(18, 3);

            entity.HasIndex(e => e.NormalizedSku);
            entity.HasIndex(e => e.MinPrice);
            entity.HasIndex(e => e.Title);

            entity.HasOne(e => e.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CheapestRetailer)
                .WithMany()
                .HasForeignKey(e => e.CheapestRetailerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ProductListing (Store Offers)
        modelBuilder.Entity<ProductListing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RetailerSku).IsRequired().HasMaxLength(150);
            entity.Property(e => e.RetailerProductId).HasMaxLength(150);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ProductUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(1000);
            entity.Property(e => e.StockStatusText).HasMaxLength(100);
            entity.Property(e => e.RawManufacturer).HasMaxLength(150);
            entity.Property(e => e.RawCategory).HasMaxLength(250);

            entity.Property(e => e.RegularPrice).HasPrecision(18, 3);
            entity.Property(e => e.FinalPrice).HasPrecision(18, 3);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);

            // Composite unique index for idempotent upsert
            entity.HasIndex(e => new { e.RetailerId, e.RetailerSku }).IsUnique();
            entity.HasIndex(e => new { e.RetailerId, e.RetailerProductId });
            entity.HasIndex(e => new { e.IsInStock, e.FinalPrice });

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Listings)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Retailer)
                .WithMany(r => r.Listings)
                .HasForeignKey(e => e.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PriceHistory (Time-series analytics)
        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegularPrice).HasPrecision(18, 3);
            entity.Property(e => e.FinalPrice).HasPrecision(18, 3);

            entity.HasIndex(e => new { e.ProductListingId, e.RecordedAt });
            entity.HasIndex(e => new { e.ProductId, e.RecordedAt });
            entity.HasIndex(e => e.RecordedAt);

            entity.HasOne(e => e.ProductListing)
                .WithMany(pl => pl.PriceHistories)
                .HasForeignKey(e => e.ProductListingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.PriceHistories)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Retailer)
                .WithMany(r => r.PriceHistories)
                .HasForeignKey(e => e.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ScrapeSession (Scraping logs and metrics)
        modelBuilder.Entity<ScrapeSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => new { e.RetailerId, e.StartedAt });

            entity.HasOne(e => e.Retailer)
                .WithMany(r => r.ScrapeSessions)
                .HasForeignKey(e => e.RetailerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

