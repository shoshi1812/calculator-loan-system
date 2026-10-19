using CalculatorLending.Core.Entities;

namespace CalculatorLending.Core.Interfaces.Security;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(Member member);
}
