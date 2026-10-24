using System.Security.Claims;
using CalculatorLending.API.Security;
using CalculatorLending.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorLending.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentMemberId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id)
                ? id
                : throw new InvalidOperationException("The authenticated user has no valid member id claim.");
        }
    }

    protected bool IsAdmin => User.IsInRole(AppRoles.Admin);

    protected IActionResult ToActionResult<T>(ServiceResult<T> result, Func<T, IActionResult> onSuccess)
    {
        return result.Status == ResultStatus.Success
            ? onSuccess(result.Data!)
            : ToProblem(result.Status, result.Error);
    }

    protected IActionResult ToActionResult(ServiceResult result, Func<IActionResult> onSuccess)
    {
        return result.Status == ResultStatus.Success
            ? onSuccess()
            : ToProblem(result.Status, result.Error);
    }

    private ObjectResult ToProblem(ResultStatus status, string? error)
    {
        var statusCode = status switch
        {
            ResultStatus.Validation => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unexpected result status.")
        };

        return Problem(detail: error, statusCode: statusCode);
    }
}
