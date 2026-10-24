using System.Globalization;
using NewsScore.Api.Contracts;
using NewsScore.Api.Domain;
using NewsScore.Api.Validation;

namespace NewsScore.Api.Tests;

public class NewsScoreRequestValidatorTests
{
    private static NewsScoreRequest Request(params MeasurementDto?[] measurements) => new(measurements);

    private static MeasurementDto Temp(decimal? value) => new("TEMP", value);
    private static MeasurementDto Hr(decimal? value) => new("HR", value);
    private static MeasurementDto Rr(decimal? value) => new("RR", value);

    [Fact]
    public void ValidRequest_ReturnsMappedMeasurements()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(39), Hr(43), Rr(19)), out var measurements, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal(
            [
                new Measurement(MeasurementType.TEMP, 39),
                new Measurement(MeasurementType.HR, 43),
                new Measurement(MeasurementType.RR, 19),
            ],
            measurements);
    }

    [Fact]
    public void ValidRequest_InAnyOrder_IsValid()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Rr(19), Temp(39), Hr(43)), out _, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void NullRequest_ReturnsMeasurementsError()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(null, out var measurements, out var errors);

        Assert.False(isValid);
        Assert.Empty(measurements);
        AssertSingleError(errors, "measurements", "Measurements are required.");
    }

    [Fact]
    public void NullMeasurementsList_ReturnsMeasurementsError()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(new NewsScoreRequest(null), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements", "Measurements are required.");
    }

    [Fact]
    public void EmptyMeasurementsList_ReportsAllTypesMissing()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(Request(), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements", "Missing measurement types: TEMP, HR, RR.");
    }

    [Fact]
    public void MissingType_ReportsWhichTypeIsMissing()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(Request(Temp(39), Hr(43)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements", "Missing measurement types: RR.");
    }

    [Fact]
    public void NullMeasurementItem_ReturnsErrorForThatIndex()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(39), null, Hr(43), Rr(19)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements[1]", "Measurement is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankType_ReturnsTypeRequired(string? type)
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(39), Hr(43), Rr(19), new MeasurementDto(type, 50)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements[3].type", "Type is required.");
    }

    [Theory]
    [InlineData("SPO2")]
    [InlineData("temp")]
    [InlineData("1")]
    public void UnknownType_ReturnsUnknownTypeError(string type)
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(39), Hr(43), Rr(19), new MeasurementDto(type, 50)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements[3].type", $"Unknown type '{type}'. Expected one of: TEMP, HR, RR.");
    }

    [Fact]
    public void DuplicateType_ReturnsErrorOnSecondOccurrence()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(39), Hr(43), Rr(19), Hr(60)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements[3].type", "Duplicate type 'HR'. Each type must appear exactly once.");
    }

    [Fact]
    public void MissingValue_ReturnsValueRequired()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(null), Hr(43), Rr(19)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements[0].value", "Value is required.");
    }

    [Theory]
    [InlineData("36.5")]
    [InlineData("36.0")]
    public void NonIntegerValue_ReturnsWholeNumberError(string value)
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(decimal.Parse(value, CultureInfo.InvariantCulture)), Hr(43), Rr(19)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements[0].value", "Value must be a whole number without decimals.");
    }

    [Theory]
    [InlineData("TEMP", 31, "TEMP must be between 32 and 42.")]
    [InlineData("TEMP", 43, "TEMP must be between 32 and 42.")]
    [InlineData("HR", 25, "HR must be between 26 and 220.")]
    [InlineData("HR", 221, "HR must be between 26 and 220.")]
    [InlineData("RR", 3, "RR must be between 4 and 60.")]
    [InlineData("RR", 61, "RR must be between 4 and 60.")]
    public void ValueOutsideAllBands_ReturnsRangeError(string type, int value, string expectedMessage)
    {
        MeasurementDto[] measurements = [Temp(39), Hr(43), Rr(19)];
        var index = Array.FindIndex(measurements, m => m.Type == type);
        measurements[index] = new MeasurementDto(type, value);

        var isValid = NewsScoreRequestValidator.TryValidate(Request(measurements), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, $"measurements[{index}].value", expectedMessage);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("100000000000000000000")]
    public void ValueFarOutsideIntRange_ReturnsRangeErrorWithoutThrowing(string value)
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(Temp(decimal.Parse(value, CultureInfo.InvariantCulture)), Hr(43), Rr(19)), out _, out var errors);

        Assert.False(isValid);
        AssertSingleError(errors, "measurements[0].value", "TEMP must be between 32 and 42.");
    }

    [Fact]
    public void InvalidValue_StillCountsTypeAsPresent()
    {
        NewsScoreRequestValidator.TryValidate(Request(Temp(50), Hr(43), Rr(19)), out _, out var errors);

        Assert.False(errors.ContainsKey("measurements"));
    }

    [Fact]
    public void MultipleProblems_AreAllReportedAtOnce()
    {
        var isValid = NewsScoreRequestValidator.TryValidate(
            Request(new MeasurementDto("SPO2", 98), Hr(300), Rr(36.5m)), out var measurements, out var errors);

        Assert.False(isValid);
        Assert.Empty(measurements);
        Assert.Equal(
            new Dictionary<string, string[]>
            {
                ["measurements[0].type"] = ["Unknown type 'SPO2'. Expected one of: TEMP, HR, RR."],
                ["measurements[1].value"] = ["HR must be between 26 and 220."],
                ["measurements[2].value"] = ["Value must be a whole number without decimals."],
                ["measurements"] = ["Missing measurement types: TEMP."],
            },
            errors);
    }

    private static void AssertSingleError(Dictionary<string, string[]> errors, string key, string message)
    {
        var error = Assert.Single(errors);
        Assert.Equal(key, error.Key);
        Assert.Equal([message], error.Value);
    }
}
