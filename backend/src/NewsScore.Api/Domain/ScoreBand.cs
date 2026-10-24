namespace NewsScore.Api.Domain;

public sealed record ScoreBand(int LowerExclusive, int UpperInclusive, int Score)
{
    public bool Contains(int value) => value > LowerExclusive && value <= UpperInclusive;
}
