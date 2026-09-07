using System;

namespace Hi_Trade.Models.DTOs;

public record RetailerOfferDto
{
    public int ListingId { get; init; }
    public int RetailerId { get; init; }
    public string RetailerCode { get; init; } = string.Empty;
    public string RetailerName { get; init; } = string.Empty;
    public string? RetailerLogoUrl { get; init; }
    public string RetailerSku { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ProductUrl { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal RegularPrice { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal DiscountPercentage { get; init; }
    public bool IsInStock { get; init; }
    public string? StockStatusText { get; init; }
    
    // Comparison metrics calculated against the cheapest offer
    public bool IsCheapest { get; init; }
    public decimal DifferenceVsCheapest { get; init; }
    public decimal DifferencePercentage { get; init; }
    
    public DateTime LastScrapedAt { get; init; }
}

