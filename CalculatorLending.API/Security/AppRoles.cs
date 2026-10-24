using CalculatorLending.Core.Enums;

namespace CalculatorLending.API.Security;

public static class AppRoles
{
    // nameof keeps the values identical to the MemberRole enum names
    public const string Admin = nameof(MemberRole.Admin);
    public const string Member = nameof(MemberRole.Member);
}
