using System.ComponentModel.DataAnnotations;

namespace CalculatorLending.Core.DTOs.Items;

public class ItemQueryParameters
{
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public bool SortDescending { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}
