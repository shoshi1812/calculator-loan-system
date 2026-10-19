using System.ComponentModel.DataAnnotations;

namespace CalculatorLending.Core.DTOs.Loans;

public class LoanCreateRequest
{
    [Range(1, int.MaxValue)]
    public int ItemId { get; set; }
}
