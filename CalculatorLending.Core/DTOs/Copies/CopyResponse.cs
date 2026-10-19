namespace CalculatorLending.Core.DTOs.Copies;

public class CopyResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
