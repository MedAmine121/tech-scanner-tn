using System;
using System.Collections.Generic;

namespace Hi_Trade.Models.DTOs;

public record RetailerCompetitivenessDto
{
    public int RetailerId { get; init; }
    public string RetailerCode { get; init; } = string.Empty;
    public string RetailerName { get; init; } = string.Empty;
    public int TotalOffersCount { get; init; }
    public int InStockOffersCount { get; init; }
    public int CheapestOffersCount { get; init; }
    public decimal CheapestMarketSharePercentage { get; init; }
    public decimal OutOfStockRatePercentage { get; init; }
    public decimal AverageDiscountPercentage { get; init; }
}

public record MarketOverviewDto
{
    public int TotalCanonicalProducts { get; init; }
    public int TotalListingsTracked { get; init; }
    public int PriceChangesRecordedLast24h { get; init; }
    public int ProductsAtAllTimeLow { get; init; }
    public IReadOnlyList<RetailerCompetitivenessDto> RetailerStats { get; init; } = Array.Empty<RetailerCompetitivenessDto>();
}

