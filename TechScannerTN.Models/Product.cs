using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

public sealed class Product
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductReference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsInStock { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Providers Provider { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public Category? Category { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ErpStock { get; set; } = string.Empty;
}
public enum Providers
{
    Mytek,
    Tunisianet,
    SpaceNet
}
