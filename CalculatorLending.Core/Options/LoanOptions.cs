namespace CalculatorLending.Core.Options;

public class LoanOptions
{
    public int MaxActiveLoansPerMember { get; set; } = 2;
    public int LoanPeriodMonths { get; set; } = 6;
}
