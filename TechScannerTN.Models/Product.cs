using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

/// <summary>
/// Represents a canonical (master) product catalog item.
/// Multiple retailer listings (offers) map to this canonical product.
/// </summary>
public class Product
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Clean, normalized manufacturer reference / SKU (uppercase, alphanumeric only).
    /// Used for automatic cross-store matching.
    /// </summary>
    [MaxLength(150)]
    public string? NormalizedSku { get; set; }

    /// <summary>
    /// European Article Number / UPC Barcode if available.
    /// </summary>
    [MaxLength(50)]
    public string? Ean { get; set; }

    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    [MaxLength(1000)]
    public string? PrimaryImageUrl { get; set; }

    public string? Description { get; set; }

    public string? Specifications { get; set; }

    // Pre-computed price comparison aggregates for sub-millisecond query performance
    [Column(TypeName = "decimal(18, 3)")]
    public decimal MinPrice { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal MaxPrice { get; set; }

    public int? CheapestRetailerId { get; set; }
    public Retailer? CheapestRetailer { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal HistoricalLowPrice { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal HistoricalHighPrice { get; set; }

    public int OffersCount { get; set; }

    public DateTime LastPriceCheckAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ProductListing> Listings { get; set; } = new List<ProductListing>();
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}

