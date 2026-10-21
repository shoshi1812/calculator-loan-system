using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Interfaces.Security;
using CalculatorLending.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CalculatorLending.Service.Security;

public class TokenService : ITokenService
{
    private const int MinSecretKeyLength = 48;

    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(Member member)
    {
        if (_options.SecretKey.Length < MinSecretKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey must be at least {MinSecretKeyLength} characters long.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new Claim(ClaimTypes.Email, member.Email),
            new Claim(ClaimTypes.Name, member.FullName),
            new Claim(ClaimTypes.Role, member.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
