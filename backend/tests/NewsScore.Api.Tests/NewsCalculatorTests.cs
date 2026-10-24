using NewsScore.Api.Domain;

namespace NewsScore.Api.Tests;

public class NewsCalculatorTests
{
    [Theory]
    [InlineData(MeasurementType.TEMP, 32, 3)]
    [InlineData(MeasurementType.TEMP, 35, 3)]
    [InlineData(MeasurementType.TEMP, 36, 1)]
    [InlineData(MeasurementType.TEMP, 37, 0)]
    [InlineData(MeasurementType.TEMP, 38, 0)]
    [InlineData(MeasurementType.TEMP, 39, 1)]
    [InlineData(MeasurementType.TEMP, 40, 2)]
    [InlineData(MeasurementType.TEMP, 42, 2)]

    [InlineData(MeasurementType.HR, 26, 3)]
    [InlineData(MeasurementType.HR, 40, 3)]
    [InlineData(MeasurementType.HR, 41, 1)]
    [InlineData(MeasurementType.HR, 50, 1)]
    [InlineData(MeasurementType.HR, 51, 0)]
    [InlineData(MeasurementType.HR, 90, 0)]
    [InlineData(MeasurementType.HR, 91, 1)]
    [InlineData(MeasurementType.HR, 110, 1)]
    [InlineData(MeasurementType.HR, 111, 2)]
    [InlineData(MeasurementType.HR, 130, 2)]
    [InlineData(MeasurementType.HR, 131, 3)]
    [InlineData(MeasurementType.HR, 220, 3)]

    [InlineData(MeasurementType.RR, 4, 3)]
    [InlineData(MeasurementType.RR, 8, 3)]
    [InlineData(MeasurementType.RR, 9, 1)]
    [InlineData(MeasurementType.RR, 11, 1)]
    [InlineData(MeasurementType.RR, 12, 0)]
    [InlineData(MeasurementType.RR, 20, 0)]
    [InlineData(MeasurementType.RR, 21, 2)]
    [InlineData(MeasurementType.RR, 24, 2)]
    [InlineData(MeasurementType.RR, 25, 3)]
    [InlineData(MeasurementType.RR, 60, 3)]
    public void ScoreFor_ValueInsideABand_ReturnsThatBandsScore(MeasurementType type, int value, int expected)
    {
        Assert.Equal(expected, NewsCalculator.ScoreFor(type, value));
    }

    [Theory]
    [InlineData(MeasurementType.TEMP, 31)]
    [InlineData(MeasurementType.TEMP, 43)]
    [InlineData(MeasurementType.HR, 25)]
    [InlineData(MeasurementType.HR, 221)]
    [InlineData(MeasurementType.RR, 3)]
    [InlineData(MeasurementType.RR, 61)]
    public void ScoreFor_ValueOutsideAllBands_Throws(MeasurementType type, int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewsCalculator.ScoreFor(type, value));
    }

    [Fact]
    public void Calculate_SpecExample_ReturnsTwo()
    {
        Measurement[] measurements =
        [
            new(MeasurementType.TEMP, 39),
            new(MeasurementType.HR, 43),
            new(MeasurementType.RR, 19),
        ];

        Assert.Equal(2, NewsCalculator.Calculate(measurements));
    }

    [Fact]
    public void Calculate_AllMeasurementsInHighestBands_ReturnsSumOfScores()
    {
        Measurement[] measurements =
        [
            new(MeasurementType.TEMP, 42),
            new(MeasurementType.HR, 220),
            new(MeasurementType.RR, 60),
        ];

        Assert.Equal(8, NewsCalculator.Calculate(measurements));
    }
}
