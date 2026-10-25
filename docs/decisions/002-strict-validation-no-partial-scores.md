# ADR 002: Strict validation, no partial scores

## Context

The NEWS score drives escalation for patients at risk of rapid deterioration. If a vital sign is missing or unreadable and silently counts as 0, the total looks better than it is, which could hide a deteriorating patient.

The spec says each value is an integer and values outside the ranges are invalid. It does not say what happens when a type is missing, repeated or unknown, or when a value is not a whole number.

## Decision

A request produces a score only when it is fully valid. Otherwise the API returns `400` with every problem it found.

- **TEMP, HR and RR are each required exactly once.** Missing, duplicate and unknown types are errors. Type codes are case-sensitive.
- **Values must be whole numbers.** `36.5` and `36.0` are rejected, and so is a number sent as a string (`"39"`), because System.Text.Json's number handling is set to `Strict`.
- **Out-of-range values are rejected**, using the score table (see ADR 001).
- **All errors are collected in one pass**, so a client can fix everything in one round trip.

To make precise messages possible, the request types are lenient and the domain types are strict. `MeasurementDto` has `string? Type` and `decimal? Value`, so input like `"SPO2"` or `36.5` still reaches the validator instead of failing as a generic JSON error. Only after validation is it mapped to the domain's `MeasurementType` enum and `int`.

## Alternatives considered

- **Score whatever is present.** Rejected: a partial score looks like a real one, and a missing value treated as 0 is the failure this decision exists to prevent.
- **Stop at the first error.** Simpler, but the user would fix one field per round trip.
- **Accept decimals and round them.** Rejected: rounding near a band boundary (for example 38.5 °C) changes the score, and that is a clinical decision, not a technical one.

## Consequences

- No request with a missing, repeated or unreadable vital sign can produce a score.
- Clients always get specific, per-field messages (see ADR 003).
- Clients must always send all three measurements.
- Real temperatures are measured to one decimal, and the spec's integer rule rejects them. This should be revisited with clinicians before production.
- The frontend checks only that a field is filled in and is a whole number. Everything else, including the ranges, is checked here, so the clinical rules exist in one place.
