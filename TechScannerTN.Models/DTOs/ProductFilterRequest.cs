using System;

namespace Hi_Trade.Models.DTOs;

public class ProductFilterRequest
{
    public string? SearchTerm { get; set; }
    public int? BrandId { get; set; }
    public int? CategoryId { get; set; }
    public int? RetailerId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? OnlyInStock { get; set; }
    public bool? OnlyHistoricalLows { get; set; }
    public string? SortBy { get; set; } = "cheapest"; // cheapest, expensive, name, newest, savings
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

