using System.ComponentModel.DataAnnotations;

namespace CalculatorLending.Core.DTOs.Copies;

public class CopyCreateRequest
{
    [Required]
    [StringLength(50)]
    public string SerialNumber { get; set; } = string.Empty;
}
