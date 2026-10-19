using System.ComponentModel.DataAnnotations;

namespace CalculatorLending.Core.DTOs.Loans;

public class LoanQueryParameters
{
    public bool? Overdue { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}
