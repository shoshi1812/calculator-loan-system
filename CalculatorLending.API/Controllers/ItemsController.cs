using CalculatorLending.API.Security;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Copies;
using CalculatorLending.Core.DTOs.Items;
using CalculatorLending.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorLending.API.Controllers;

[Route("api/items")]
public class ItemsController : ApiControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaged([FromQuery] ItemQueryParameters query, CancellationToken ct)
    {
        var result = await _itemService.GetPagedAsync(query, ct);
        return ToActionResult(result, data => Ok(data));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _itemService.GetByIdAsync(id, ct);
        return ToActionResult(result, data => Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(ItemCreateRequest request, CancellationToken ct)
    {
        var result = await _itemService.CreateAsync(request, ct);
        return ToActionResult(result, data => CreatedAtAction(nameof(GetById), new { id = data.Id }, data));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, ItemUpdateRequest request, CancellationToken ct)
    {
        var result = await _itemService.UpdateAsync(id, request, ct);
        return ToActionResult(result, () => NoContent());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _itemService.DeleteAsync(id, ct);
        return ToActionResult(result, () => NoContent());
    }

    [HttpGet("{itemId:int}/copies")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<CopyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCopies(int itemId, CancellationToken ct)
    {
        var result = await _itemService.GetCopiesAsync(itemId, ct);
        return ToActionResult(result, data => Ok(data));
    }

    [HttpPost("{itemId:int}/copies")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(CopyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCopy(int itemId, CopyCreateRequest request, CancellationToken ct)
    {
        var result = await _itemService.AddCopyAsync(itemId, request, ct);
        return ToActionResult(result, data => CreatedAtAction(nameof(GetCopies), new { itemId }, data));
    }
}
