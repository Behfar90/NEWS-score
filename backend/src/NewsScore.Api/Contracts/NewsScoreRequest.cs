namespace NewsScore.Api.Contracts;

public sealed record NewsScoreRequest(IReadOnlyList<MeasurementDto?>? Measurements);
