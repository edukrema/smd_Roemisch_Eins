namespace Game.Api.Errors;

using Game.Api.Data;
using Game.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

// REQ19
/// <summary>The central place where exceptions become ProblemDetails.</summary>
public class ProblemExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ProblemExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Invalid input", exception.Message),
            BadHttpRequestException e => (e.StatusCode, "Invalid request", exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Not signed in", exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, "No permission", exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
            DomainException => (StatusCodes.Status409Conflict, "Rule violation", exception.Message),
            DbUpdateException e when e.IsUniqueViolation() =>
                (StatusCodes.Status409Conflict, "Conflict", "The action conflicts with the current state."),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", null)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unexpected error on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Status = status, Title = title, Detail = detail }
        });
    }
}
