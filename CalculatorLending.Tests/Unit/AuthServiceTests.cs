using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Auth;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Core.Interfaces.Security;
using CalculatorLending.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CalculatorLending.Tests.Unit;

public class AuthServiceTests
{
    private const string PlainPassword = "Password123";
    private const string HashedPassword = "hashed-password";
    private const string Token = "test-token";

    private static readonly DateTime ExpiresAt = new(2026, 10, 18, 13, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IMemberRepository> _members = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public AuthServiceTests()
    {
        _passwordHasher.Setup(h => h.Hash(PlainPassword)).Returns(HashedPassword);
        _passwordHasher.Setup(h => h.Verify(PlainPassword, HashedPassword)).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<Member>())).Returns((Token, ExpiresAt));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AuthService CreateService() => new(
        _members.Object,
        _unitOfWork.Object,
        _passwordHasher.Object,
        _tokenService.Object,
        NullLogger<AuthService>.Instance);

    private static Member CreateMember() => new()
    {
        Id = 1,
        FullName = "Test Member",
        Email = "member@example.com",
        PasswordHash = HashedPassword,
        Role = MemberRole.Member
    };

    private void SetupExistingMember()
    {
        _members.Setup(m => m.GetByEmailAsync("member@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember());
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_ReturnsTokenAndStoresHash()
    {
        Member? added = null;
        _members.Setup(m => m.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _members.Setup(m => m.Add(It.IsAny<Member>()))
            .Callback<Member>(m => added = m);
        var request = new RegisterRequest
        {
            FullName = "New Member",
            Email = "  New.Member@Example.COM ",
            Password = PlainPassword
        };
        var service = CreateService();

        var result = await service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(Token, result.Data!.Token);
        Assert.NotNull(added);
        Assert.NotEqual(PlainPassword, added.PasswordHash);
        Assert.Equal(HashedPassword, added.PasswordHash);
        Assert.Equal("new.member@example.com", added.Email);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsConflict()
    {
        _members.Setup(m => m.EmailExistsAsync("member@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new RegisterRequest
        {
            FullName = "Test Member",
            Email = "member@example.com",
            Password = PlainPassword
        };
        var service = CreateService();

        var result = await service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _members.Verify(m => m.Add(It.IsAny<Member>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
    {
        SetupExistingMember();
        var service = CreateService();

        var result = await service.LoginAsync(
            new LoginRequest { Email = "member@example.com", Password = "WrongPassword" },
            CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsUnauthorized()
    {
        var service = CreateService();

        var result = await service.LoginAsync(
            new LoginRequest { Email = "unknown@example.com", Password = PlainPassword },
            CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmailAndWrongPassword_ReturnSameMessage()
    {
        SetupExistingMember();
        var service = CreateService();

        var wrongPassword = await service.LoginAsync(
            new LoginRequest { Email = "member@example.com", Password = "WrongPassword" },
            CancellationToken.None);
        var unknownEmail = await service.LoginAsync(
            new LoginRequest { Email = "unknown@example.com", Password = PlainPassword },
            CancellationToken.None);

        Assert.Equal(wrongPassword.Error, unknownEmail.Error);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsToken()
    {
        SetupExistingMember();
        var service = CreateService();

        var result = await service.LoginAsync(
            new LoginRequest { Email = "member@example.com", Password = PlainPassword },
            CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(Token, result.Data!.Token);
        Assert.Equal(ExpiresAt, result.Data.ExpiresAt);
    }
}