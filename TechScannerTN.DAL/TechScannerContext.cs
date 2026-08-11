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
    }
}
