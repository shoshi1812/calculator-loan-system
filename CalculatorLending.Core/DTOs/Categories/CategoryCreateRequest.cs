using System.ComponentModel.DataAnnotations;

namespace CalculatorLending.Core.DTOs.Categories;

public class CategoryCreateRequest
{
    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;
}
