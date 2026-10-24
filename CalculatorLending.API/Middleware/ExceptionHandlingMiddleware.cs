using CalculatorLending.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorLending.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            // The client closed the connection; not an error
        }
        // --- Open topic 3: safety net per doc 00 section 9 ---
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "Conflict",
                "The resource was changed by another request. Please try again.");
        }
        // --- End of open topic 3 ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var detail = _environment.IsDevelopment()
                ? ex.ToString()
                : "An unexpected error occurred. Please try again later.";

            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error", detail);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
        problem.Extensions["correlationId"] = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString();

        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
