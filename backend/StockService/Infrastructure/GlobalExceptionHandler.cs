using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StockService.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var apiException = exception as ApiException;
        if (apiException is null)
        {
            logger.LogError(exception, "Unexpected error while processing {Path}", httpContext.Request.Path);
        }

        var status = apiException?.StatusCode ?? StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = apiException?.Title ?? "Erro inesperado",
            Detail = apiException?.Message ?? "Não foi possível concluir a operação.",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = apiException?.Code ?? "unexpected_error";

        httpContext.Response.StatusCode = status;
        var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });

        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }

        return true;
    }
}
