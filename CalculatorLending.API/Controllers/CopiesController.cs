using CalculatorLending.API.Security;
using CalculatorLending.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorLending.API.Controllers;

[Route("api/copies")]
public class CopiesController : ApiControllerBase
{
    private readonly IItemService _itemService;

    public CopiesController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _itemService.DeleteCopyAsync(id, ct);
        return ToActionResult(result, () => NoContent());
    }
}
