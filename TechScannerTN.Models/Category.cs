using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hi_Trade.Models;

public class Category
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty; // e.g. "Ordinateurs Portables", "Smartphones"

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty; // e.g. "ordinateurs-portables", "smartphones"

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<RetailerCategory> RetailerCategories { get; set; } = new List<RetailerCategory>();
}

