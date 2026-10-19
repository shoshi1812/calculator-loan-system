namespace CalculatorLending.Core.Entities;

public class Loan
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int ItemCopyId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    public bool IsActive => ReturnDate == null;

    public Member Member { get; set; } = null!;
    public ItemCopy ItemCopy { get; set; } = null!;
}
