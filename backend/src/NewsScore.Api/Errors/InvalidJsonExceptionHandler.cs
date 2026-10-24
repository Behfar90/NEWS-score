using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace NewsScore.Api.Errors;

public sealed class InvalidJsonExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const string BodyKey = "body";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException { InnerException: JsonException jsonException })
        {
            return false;
        }

        var (key, message) = ToFieldError(jsonException);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new HttpValidationProblemDetails(new Dictionary<string, string[]> { [key] = [message] }),
            Exception = exception,
        });
    }

    private static (string Key, string Message) ToFieldError(JsonException exception)
    {
        if (exception.InnerException is JsonException)
        {
            return (BodyKey, "The request body is not valid JSON.");
        }

        if (exception.Path is null or "$")
        {
            return (BodyKey, "The request body must be a JSON object with a 'measurements' array.");
        }

        var key = exception.Path.TrimStart('$', '.');

        if (key.Equals("measurements", StringComparison.OrdinalIgnoreCase))
        {
            return (key, "Measurements must be an array.");
        }

        if (key.EndsWith(".type", StringComparison.OrdinalIgnoreCase))
        {
            return (key, "Type must be a string.");
        }

        if (key.EndsWith(".value", StringComparison.OrdinalIgnoreCase))
        {
            return (key, "Value must be a number.");
        }

        return (key, "Invalid JSON value.");
    }
}
