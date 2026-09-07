using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

/// <summary>
/// Represents a specific store's product listing/offer (e.g. on MyTek, TunisiaNet, SpaceNet).
/// Linked to a canonical Product once matched.
/// </summary>
public class ProductListing
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int RetailerId { get; set; }
    public Retailer Retailer { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string RetailerSku { get; set; } = string.Empty; // Store reference/SKU

    [MaxLength(150)]
    public string RetailerProductId { get; set; } = string.Empty; // Store internal id

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string ProductUrl { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal RegularPrice { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal FinalPrice { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal DiscountPercentage { get; set; }

    public bool IsInStock { get; set; }

    [MaxLength(100)]
    public string? StockStatusText { get; set; }

    [MaxLength(150)]
    public string? RawManufacturer { get; set; }

    [MaxLength(250)]
    public string? RawCategory { get; set; }

    public DateTime FirstScrapedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastScrapedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastPriceChangedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}

