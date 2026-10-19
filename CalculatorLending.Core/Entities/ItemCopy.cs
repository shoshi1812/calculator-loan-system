using CalculatorLending.Core.Enums;

namespace CalculatorLending.Core.Entities;

public class ItemCopy
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public CopyStatus Status { get; set; }
    public uint Version { get; set; }

    public Item Item { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
