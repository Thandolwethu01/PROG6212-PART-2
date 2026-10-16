using System.ComponentModel.DataAnnotations;


namespace RaceDay.DTOs.API.DTOs;

    public class CategoryDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Range(0.01, 1000)]
    public decimal Distance { get; set; }

    [Range(1, 120)]
    public int? AgeRestriction { get; set; }
}

