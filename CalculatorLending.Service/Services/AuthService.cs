using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Auth;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Core.Interfaces.Security;
using CalculatorLending.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CalculatorLending.Service.Services;

public class AuthService : IAuthService
{
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IMemberRepository members,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _members = members;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest r, CancellationToken ct)
    {
        var email = Normalize(r.Email);

        if (await _members.EmailExistsAsync(email, ct))
        {
            _logger.LogWarning("Registration rejected: email is already registered");
            return ServiceResult<AuthResponse>.Conflict("Email is already registered");
        }

        var member = new Member
        {
            FullName = r.FullName,
            Email = email,
            PasswordHash = _passwordHasher.Hash(r.Password),
            Role = MemberRole.Member
        };
        _members.Add(member);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // Two registrations with the same email at the same moment
            _logger.LogWarning("Registration rejected: duplicate email detected on save");
            return ServiceResult<AuthResponse>.Conflict("Email is already registered");
        }

        _logger.LogInformation("Member {MemberId} registered", member.Id);
        return ServiceResult<AuthResponse>.Success(BuildResponse(member));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest r, CancellationToken ct)
    {
        var member = await _members.GetByEmailAsync(Normalize(r.Email), ct);

        // Same message for an unknown email and a wrong password, so registered emails are not revealed
        if (member is null || !_passwordHasher.Verify(r.Password, member.PasswordHash))
        {
            _logger.LogWarning("Login rejected: invalid credentials");
            return ServiceResult<AuthResponse>.Unauthorized("Invalid email or password");
        }

        _logger.LogInformation("Member {MemberId} logged in", member.Id);
        return ServiceResult<AuthResponse>.Success(BuildResponse(member));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private AuthResponse BuildResponse(Member member)
    {
        var (token, expiresAt) = _tokenService.GenerateToken(member);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            FullName = member.FullName,
            Role = member.Role.ToString()
        };
    }
}
