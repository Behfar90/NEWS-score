using NewsScore.Api.Domain;

namespace NewsScore.Api.Tests;

public class ScoreBandTests
{
    private static readonly ScoreBand Band = new(LowerExclusive: 35, UpperInclusive: 36, Score: 1);

    [Theory]
    [InlineData(34, false)]
    [InlineData(35, false)] 
    [InlineData(36, true)]  
    [InlineData(37, false)]
    public void Contains_UsesExclusiveStartAndInclusiveEnd(int value, bool expected)
    {
        Assert.Equal(expected, Band.Contains(value));
    }
}
