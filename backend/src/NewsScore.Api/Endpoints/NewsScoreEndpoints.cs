using Microsoft.AspNetCore.Http.HttpResults;
using NewsScore.Api.Contracts;
using NewsScore.Api.Domain;
using NewsScore.Api.Validation;

namespace NewsScore.Api.Endpoints;

public static class NewsScoreEndpoints
{
    public static void MapNewsScoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/news-score", CalculateScore);
    }

    private static Results<Ok<NewsScoreResponse>, ValidationProblem> CalculateScore(NewsScoreRequest? request)
    {
        if (!NewsScoreRequestValidator.TryValidate(request, out var measurements, out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        return TypedResults.Ok(new NewsScoreResponse(NewsCalculator.Calculate(measurements)));
    }
}
