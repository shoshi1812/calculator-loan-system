using CalculatorLending.API.Security;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorLending.API.Controllers;

[Route("api/loans")]
public class LoansController : ApiControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Member)]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(LoanCreateRequest request, CancellationToken ct)
    {
        var result = await _loanService.CreateAsync(CurrentMemberId, request, ct);
        return ToActionResult(result, data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _loanService.GetByIdAsync(id, CurrentMemberId, IsAdmin, ct);
        return ToActionResult(result, data => Ok(data));
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Member)]
    [ProducesResponseType(typeof(PagedResult<LoanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMine([FromQuery] LoanQueryParameters query, CancellationToken ct)
    {
        var result = await _loanService.GetMineAsync(CurrentMemberId, query, ct);
        return ToActionResult(result, data => Ok(data));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(PagedResult<LoanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] LoanQueryParameters query, CancellationToken ct)
    {
        var result = await _loanService.GetAllAsync(query, ct);
        return ToActionResult(result, data => Ok(data));
    }

    // The body is validated by [ApiController] (Returned must be true); the service needs only the ids
    [HttpPatch("{id:int}")]
    [Authorize(Roles = AppRoles.Member)]
    [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Return(int id, LoanReturnRequest request, CancellationToken ct)
    {
        var result = await _loanService.ReturnAsync(CurrentMemberId, id, ct);
        return ToActionResult(result, data => Ok(data));
    }
}
