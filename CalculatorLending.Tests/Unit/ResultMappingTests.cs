using CalculatorLending.API.Controllers;
using CalculatorLending.Core.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorLending.Tests.Unit;

public class ResultMappingTests
{
    private sealed class TestController : ApiControllerBase
    {
        public IActionResult Convert<T>(ServiceResult<T> result, Func<T, IActionResult> onSuccess) =>
            ToActionResult(result, onSuccess);
    }

    private static TestController CreateController() => new()
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private static ServiceResult<string> Failure(ResultStatus status, string message) => status switch
    {
        ResultStatus.Validation => ServiceResult<string>.Validation(message),
        ResultStatus.Unauthorized => ServiceResult<string>.Unauthorized(message),
        ResultStatus.Forbidden => ServiceResult<string>.Forbidden(message),
        ResultStatus.NotFound => ServiceResult<string>.NotFound(message),
        ResultStatus.Conflict => ServiceResult<string>.Conflict(message),
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    [Fact]
    public void Conflict_Returns409ProblemDetails()
    {
        var controller = CreateController();

        var result = controller.Convert(ServiceResult<string>.Conflict("No copy is currently available"), Ok);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("No copy is currently available", problem.Detail);
    }

    [Theory]
    [InlineData(ResultStatus.Validation, 400)]
    [InlineData(ResultStatus.Unauthorized, 401)]
    [InlineData(ResultStatus.Forbidden, 403)]
    [InlineData(ResultStatus.NotFound, 404)]
    public void Failure_ReturnsMatchingStatusCode(ResultStatus status, int expectedStatusCode)
    {
        var controller = CreateController();

        var result = controller.Convert(Failure(status, "error"), Ok);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    [Fact]
    public void Success_CallsOnSuccess()
    {
        var controller = CreateController();

        var result = controller.Convert(ServiceResult<string>.Success("data"), data => new OkObjectResult(data));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("data", ok.Value);
    }

    private static IActionResult Ok(string data) => new OkObjectResult(data);
}