# ADR 001: Score bands as data

## Context

The spec defines the NEWS score as a table. Each measurement type (TEMP, HR, RR) has value ranges with a score each, where the start of a range is exclusive and the end is inclusive. Values outside every range are invalid.

Written as `if`/`else` chains, that is 16 branches, each a chance for a `<` vs `<=` mistake. Validation also needs to know the valid ranges, and if it kept its own copy, scoring and validation could drift apart.

## Decision

The table is data, and one small type holds the boundary rule:

```csharp
public sealed record ScoreBand(int LowerExclusive, int UpperInclusive, int Score)
{
    public bool Contains(int value) => value > LowerExclusive && value <= UpperInclusive;
}
```

- `NewsScoringRules.Bands` maps each `MeasurementType` to its `ScoreBand`s, one line per row of the spec table.
- `NewsCalculator` finds the band that contains the value and returns its score. If no band contains it, it throws; it never returns 0.
- The validator uses the same table. A value is valid if a band contains it, and the "must be between X and Y" message takes X and Y from the table.

## Alternatives considered

- **`if`/`switch` per type.** It works, but it repeats the boundary rule for every band and gives validation nothing to reuse.
- **Thresholds in configuration or a database.** That's useful for versioned or per-municipality rules, but more than this case needs. The same `ScoreBand` shape can be loaded from config later.

## Consequences

- The boundary rule exists in exactly one place, and the table reads like the spec, so reviewing it means comparing numbers.
- Scoring and validation cannot disagree, because they read the same data.
- Every band boundary of every type is covered by a test (30 cases plus the out-of-range cases). Changing `<=` to `<` in `Contains` makes 19 tests fail.
- Changing a threshold is a one-line diff, but it still needs a deploy. Configurable, versioned thresholds are listed as a production next step.
- The range shown in error messages (lowest start to highest end) assumes the bands for a type are contiguous, which they are in the spec. Validity itself is checked band by band, so a gap would still be rejected correctly.
