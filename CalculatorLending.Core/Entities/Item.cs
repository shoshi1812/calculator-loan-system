namespace CalculatorLending.Core.Entities;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<ItemCopy> Copies { get; set; } = new List<ItemCopy>();
}
