using System;

namespace Hi_Trade.Models.DTOs;

/// <summary>
/// Universal DTO emitted by all website scrapers (MyTek, TunisiaNet, SpaceNet, etc.).
/// </summary>
public record ScrapedProductItem
{
    public required string RetailerCode { get; init; } // e.g. "MYTEK", "TUNISIANET", "SPACENET"
    public string RetailerProductId { get; init; } = string.Empty;
    public required string RetailerSku { get; init; } // Store reference
    public required string Title { get; init; }
    public required string ProductUrl { get; init; }
    public string? ImageUrl { get; init; }
    public decimal RegularPrice { get; init; }
    public decimal FinalPrice { get; init; }
    public bool IsInStock { get; init; }
    public string? StockStatusText { get; init; }
    public string? Manufacturer { get; init; }
    public string? Description { get; init; }
    public string? RawCategory { get; init; }
    public string? CategoryUrl { get; init; }
}

