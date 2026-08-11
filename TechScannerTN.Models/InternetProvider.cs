namespace Hi_Trade.Models;

public class InternetProvider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
}
