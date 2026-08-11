namespace Hi_Trade.Models;

public class Plan
{
    public int Id { get; set; }
    public int InternetProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Speed { get; set; }
    public string SpeedUnit { get; set; } = "Mbps";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TND";
    public string? Description { get; set; }
    public string? DataLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScrapedAt { get; set; }
    
    public InternetProvider? InternetProvider { get; set; }
}
