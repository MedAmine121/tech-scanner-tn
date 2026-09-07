using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

public class Brand
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty; // e.g. "Lenovo", "Asus", "Apple", "Dell"

    [Required]
    [MaxLength(150)]
    public string Slug { get; set; } = string.Empty; // e.g. "lenovo", "asus", "apple"

    [MaxLength(1000)]
    public string? LogoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

