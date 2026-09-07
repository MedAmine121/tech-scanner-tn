using System;
using System.Collections.Generic;

namespace Hi_Trade.Models.DTOs;

public record ProductComparisonDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? NormalizedSku { get; init; }
    public string? Ean { get; init; }
    public string? BrandName { get; init; }
    public string? CategoryName { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public string? Description { get; init; }
    public string? Specifications { get; init; }
    
    // Aggregates
    public decimal CheapestPrice { get; init; }
    public decimal HighestPrice { get; init; }
    public string? CheapestRetailerName { get; init; }
    public int? CheapestRetailerId { get; init; }
    public decimal HistoricalLowPrice { get; init; }
    public decimal HistoricalHighPrice { get; init; }
    public bool IsAtHistoricalLow => CheapestPrice > 0 && CheapestPrice <= HistoricalLowPrice;
    
    public int OffersCount { get; init; }
    public decimal MaxSavingsAmount => HighestPrice > CheapestPrice ? HighestPrice - CheapestPrice : 0;
    public decimal MaxSavingsPercentage => HighestPrice > 0 && HighestPrice > CheapestPrice 
        ? Math.Round(((HighestPrice - CheapestPrice) / HighestPrice) * 100, 2) 
        : 0;
        
    public IReadOnlyList<RetailerOfferDto> Offers { get; init; } = Array.Empty<RetailerOfferDto>();
}

