using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

/// <summary>
/// Time-series record of price or stock status changes for a product listing.
/// Only written when a change actually occurs, keeping storage minimal and analytics fast.
/// </summary>
public class PriceHistory
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public int ProductListingId { get; set; }
    public ProductListing ProductListing { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int RetailerId { get; set; }
    public Retailer Retailer { get; set; } = null!;

    [Column(TypeName = "decimal(18, 3)")]
    public decimal RegularPrice { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal FinalPrice { get; set; }

    public bool IsInStock { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

