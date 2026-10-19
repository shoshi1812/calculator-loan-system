using CalculatorLending.Core.Enums;

namespace CalculatorLending.Core.Entities;

public class Member
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public MemberRole Role { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
