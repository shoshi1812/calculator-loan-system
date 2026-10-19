using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Auth;

namespace CalculatorLending.Core.Interfaces.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest r, CancellationToken ct);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest r, CancellationToken ct);
}
