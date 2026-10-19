namespace CalculatorLending.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
