using System;

namespace Hi_Trade.Models.DTOs;

public record PriceHistoryDto
{
    public long Id { get; init; }
    public int RetailerId { get; init; }
    public string RetailerName { get; init; } = string.Empty;
    public string RetailerCode { get; init; } = string.Empty;
    public decimal RegularPrice { get; init; }
    public decimal FinalPrice { get; init; }
    public bool IsInStock { get; init; }
    public DateTime RecordedAt { get; init; }
}

