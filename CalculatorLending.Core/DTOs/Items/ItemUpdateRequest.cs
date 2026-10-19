using System.ComponentModel.DataAnnotations;

namespace CalculatorLending.Core.DTOs.Items;

public class ItemUpdateRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ModelNumber { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MinLength(1)]
    public List<int> CategoryIds { get; set; } = new();
}
