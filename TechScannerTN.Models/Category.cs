namespace Hi_Trade.Models;

public sealed class Category
{
    // The Mytek category URL is stable and uniquely identifies a category.
    public string Url { get; set; } = string.Empty;
    public string ParentCategory { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
