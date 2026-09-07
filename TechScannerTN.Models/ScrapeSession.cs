using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

public enum ScrapeSessionStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    Partial = 3
}

public class ScrapeSession
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int RetailerId { get; set; }
    public Retailer Retailer { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    public ScrapeSessionStatus Status { get; set; } = ScrapeSessionStatus.Running;

    public int ItemsScraped { get; set; }
    public int ItemsUpdated { get; set; }
    public int NewItemsCount { get; set; }
    public int PriceChangesCount { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}

