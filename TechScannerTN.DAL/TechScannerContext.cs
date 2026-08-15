using Microsoft.EntityFrameworkCore;
using Hi_Trade.Models;

namespace Hi_Trade.DAL;

public class TechScannerContext : DbContext
{
    public TechScannerContext(DbContextOptions<TechScannerContext> options) : base(options)
    {
    }

    public DbSet<InternetProvider> InternetProviders { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Url);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ParentCategory).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductReference);
            entity.Property(e => e.ProductReference).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ProductUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 3);
            entity.Property(e => e.CategoryId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ScrapedAt).HasColumnName("scraped_at");
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.ScrapedAt);
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
