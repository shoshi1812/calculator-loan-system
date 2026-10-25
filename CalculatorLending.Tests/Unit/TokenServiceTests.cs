using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Options;
using CalculatorLending.Service.Security;
using Microsoft.IdentityModel.Tokens;

namespace CalculatorLending.Tests.Unit;

public class TokenServiceTests
{
    // Test-only value, not the real secret
    private const string SecretKey = "test-secret-key-for-unit-tests-only-0123456789abcdef";
    private const string Issuer = "CalculatorLending.API";
    private const string Audience = "CalculatorLending.Client";
    private const int ExpiresMinutes = 60;

    private static TokenService CreateService(string secretKey = SecretKey) => new(
        Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SecretKey = secretKey,
            ExpiresMinutes = ExpiresMinutes
        }));

    private static Member CreateMember() => new()
    {
        Id = 7,
        FullName = "Test Admin",
        Email = "admin@example.com",
        Role = MemberRole.Admin
    };

    private static ClaimsPrincipal Validate(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
    }

    [Fact]
    public void GenerateToken_ValidMember_ContainsIdEmailAndRoleClaims()
    {
        var service = CreateService();

        var (token, _) = service.GenerateToken(CreateMember());

        var principal = Validate(token);
        Assert.Equal("7", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("admin@example.com", principal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal("Admin", principal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void GenerateToken_ValidMember_ExpiresAfterConfiguredMinutes()
    {
        var service = CreateService();
        var before = DateTime.UtcNow;

        var (token, expiresAt) = service.GenerateToken(CreateMember());

        var after = DateTime.UtcNow;
        Assert.InRange(expiresAt, before.AddMinutes(ExpiresMinutes), after.AddMinutes(ExpiresMinutes));
        // The JWT stores exp in whole seconds
        var validTo = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;
        Assert.True(Math.Abs((validTo - expiresAt).TotalSeconds) < 1);
    }

    [Fact]
    public void GenerateToken_ShortSecretKey_Throws()
    {
        var service = CreateService(secretKey: "too-short");

        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(CreateMember()));
    }
}

public class PasswordHasherTests
{
    private const string Password = "Password123";

    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash(Password);

        var result = _hasher.Verify(Password, hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash(Password);

        var result = _hasher.Verify("WrongPassword", hash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ReturnsDifferentHashes()
    {
        var first = _hasher.Hash(Password);

        var second = _hasher.Hash(Password);

        Assert.NotEqual(first, second);
    }
}