namespace Game.Api.Endpoints;

public static class OpenApiExtensions
{
    /// <summary>Documents the error responses (ProblemDetails) a route can return.</summary>
    public static RouteHandlerBuilder ProducesProblems(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.ProducesProblem(statusCode);
        }

        return builder;
    }
}
