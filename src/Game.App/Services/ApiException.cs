namespace Game.App.Services;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>An error response from the Api. The message is the text from the ProblemDetails.</summary>
public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response)
    {
        var message = $"Error {(int)response.StatusCode}";
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            message = problem?.Detail ?? problem?.Title ?? message;
        }
        catch (JsonException)
        {
            // No ProblemDetails body, the default message stays.
        }

        return new ApiException(response.StatusCode, message);
    }

    private record ProblemResponse(string? Title, string? Detail);
}
