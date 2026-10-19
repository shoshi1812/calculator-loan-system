namespace CalculatorLending.Core.DTOs.Loans;

public class LoanResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int ItemCopyId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsOverdue { get; set; }
}
