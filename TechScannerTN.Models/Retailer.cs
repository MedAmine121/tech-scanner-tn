using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

public class Retailer
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty; // e.g. "MYTEK", "TUNISIANET", "SPACENET"

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty; // e.g. "MyTek", "TunisiaNet", "SpaceNet"

    [Required]
    [MaxLength(500)]
    public string BaseUrl { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? LogoUrl { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "TND";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ProductListing> Listings { get; set; } = new List<ProductListing>();
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    public ICollection<ScrapeSession> ScrapeSessions { get; set; } = new List<ScrapeSession>();
    public ICollection<RetailerCategory> RetailerCategories { get; set; } = new List<RetailerCategory>();
}

