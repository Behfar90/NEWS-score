using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace NewsScore.Api.Tests;

public class NewsScoreEndpointTests(ProductionApiFactory factory)
    : IClassFixture<ProductionApiFactory>
{
    private const string Url = "/api/news-score";

    [Fact]
    public async Task SpecExample_ReturnsScoreOnly()
    {
        var response = await PostJsonAsync("""
            {
              "measurements": [
                { "type": "TEMP", "value": 39 },
                { "type": "HR", "value": 43 },
                { "type": "RR", "value": 19 }
              ]
            }
            """);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"score":2}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InvalidMeasurement_ReturnsValidationProblemDetails()
    {
        var response = await PostJsonAsync("""
            { "measurements": [{ "type": "TEMP", "value": 31 }, { "type": "HR", "value": 43 }, { "type": "RR", "value": 19 }] }
            """);

        var problem = await ReadValidationProblemAsync(response);
        Assert.Equal(400, problem.Status);
        Assert.Equal(["TEMP must be between 32 and 42."], problem.Errors["measurements[0].value"]);
    }

    [Theory]
    [InlineData("""{ "measurements": [{ "type": "TEMP", "value": 36.0 }, { "type": "HR", "value": 43 }, { "type": "RR", "value": 19 }] }""",
        "measurements[0].value", "Value must be a whole number without decimals.")]
    [InlineData("""{ "measurements": [{ "type": "TEMP", "value": "39" }, { "type": "HR", "value": 43 }, { "type": "RR", "value": 19 }] }""",
        "measurements[0].value", "Value must be a number.")]
    [InlineData("""{ "measurements": [{ "type": 1, "value": 39 }] }""",
        "measurements[0].type", "Type must be a string.")]
    [InlineData("""{ "measurements": { "type": "TEMP" } }""",
        "measurements", "Measurements must be an array.")]
    [InlineData("""[]""",
        "body", "The request body must be a JSON object with a 'measurements' array.")]
    [InlineData("""{ "measurements": [""",
        "body", "The request body is not valid JSON.")]
    [InlineData("",
        "measurements", "Measurements are required.")]
    public async Task BadRequestBody_ReturnsFieldLevelError(string json, string expectedKey, string expectedMessage)
    {
        var response = await PostJsonAsync(json);

        var problem = await ReadValidationProblemAsync(response);
        var error = Assert.Single(problem.Errors);
        Assert.Equal(expectedKey, error.Key);
        Assert.Equal([expectedMessage], error.Value);
    }

    [Fact]
    public async Task NonJsonContentType_ReturnsUnsupportedMediaTypeProblem()
    {
        var response = await factory.CreateClient().PostAsync(Url, new StringContent("TEMP=39", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private Task<HttpResponseMessage> PostJsonAsync(string json) =>
        factory.CreateClient().PostAsync(Url, new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<HttpValidationProblemDetails> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        return problem;
    }
}
