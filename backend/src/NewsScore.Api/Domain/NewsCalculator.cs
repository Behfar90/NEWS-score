namespace NewsScore.Api.Domain;

public static class NewsCalculator
{
    public static int Calculate(IEnumerable<Measurement> measurements) =>
        measurements.Sum(m => ScoreFor(m.Type, m.Value));

    public static int ScoreFor(MeasurementType type, int value)
    {
        var band = NewsScoringRules.Bands[type].FirstOrDefault(b => b.Contains(value));

        return band?.Score
            ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"No {type} score band contains {value}.");
    }
}
