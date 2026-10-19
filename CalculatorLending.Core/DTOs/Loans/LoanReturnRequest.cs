using System.ComponentModel.DataAnnotations;

namespace CalculatorLending.Core.DTOs.Loans;

public class LoanReturnRequest
{
    [Range(typeof(bool), "true", "true", ErrorMessage = "Returned must be true.")]
    public bool Returned { get; set; }
}
