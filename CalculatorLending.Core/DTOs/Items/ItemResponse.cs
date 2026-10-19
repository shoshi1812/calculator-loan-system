using CalculatorLending.Core.DTOs.Categories;

namespace CalculatorLending.Core.DTOs.Items;

public class ItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CategoryResponse> Categories { get; set; } = new();
    public int AvailableCopies { get; set; }
}
