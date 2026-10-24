using NewsScore.Api.Contracts;
using NewsScore.Api.Domain;

namespace NewsScore.Api.Validation;

public static class NewsScoreRequestValidator
{
    private const string MeasurementsKey = "measurements";

    public static bool TryValidate(
        NewsScoreRequest? request,
        out IReadOnlyList<Measurement> measurements,
        out Dictionary<string, string[]> errors)
    {
        measurements = [];
        errors = [];

        if (request?.Measurements is null)
        {
            errors[MeasurementsKey] = ["Measurements are required."];
            return false;
        }

        var validMeasurements = new List<Measurement>();
        var seenTypes = new HashSet<MeasurementType>();

        for (var i = 0; i < request.Measurements.Count; i++)
        {
            var measurement = ValidateMeasurement(request.Measurements[i], $"{MeasurementsKey}[{i}]", seenTypes, errors);
            if (measurement is not null)
            {
                validMeasurements.Add(measurement);
            }
        }

        var missingTypes = Enum.GetValues<MeasurementType>().Where(t => !seenTypes.Contains(t)).ToList();
        if (missingTypes.Count > 0)
        {
            errors[MeasurementsKey] = [$"Missing measurement types: {string.Join(", ", missingTypes)}."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        measurements = validMeasurements;
        return true;
    }

    private static Measurement? ValidateMeasurement(
        MeasurementDto? dto,
        string key,
        HashSet<MeasurementType> seenTypes,
        Dictionary<string, string[]> errors)
    {
        if (dto is null)
        {
            errors[key] = ["Measurement is required."];
            return null;
        }

        var type = ValidateType(dto.Type, $"{key}.type", seenTypes, errors);
        var value = ValidateValue(dto.Value, $"{key}.value", errors);

        if (type is null || value is null)
        {
            return null;
        }

        if (!IsWithinScoreBands(type.Value, value.Value))
        {
            errors[$"{key}.value"] = [OutOfRangeMessage(type.Value)];
            return null;
        }

        return new Measurement(type.Value, (int)value.Value);
    }

    private static MeasurementType? ValidateType(
        string? rawType,
        string key,
        HashSet<MeasurementType> seenTypes,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            errors[key] = ["Type is required."];
            return null;
        }

        if (!Enum.GetNames<MeasurementType>().Contains(rawType))
        {
            var expected = string.Join(", ", Enum.GetNames<MeasurementType>());
            errors[key] = [$"Unknown type '{rawType}'. Expected one of: {expected}."];
            return null;
        }

        var type = Enum.Parse<MeasurementType>(rawType);

        if (!seenTypes.Add(type))
        {
            errors[key] = [$"Duplicate type '{type}'. Each type must appear exactly once."];
            return null;
        }

        return type;
    }

    private static decimal? ValidateValue(decimal? value, string key, Dictionary<string, string[]> errors)
    {
        if (value is null)
        {
            errors[key] = ["Value is required."];
            return null;
        }

        if (value.Value.Scale != 0)
        {
            errors[key] = ["Value must be a whole number without decimals."];
            return null;
        }

        return value;
    }

    private static bool IsWithinScoreBands(MeasurementType type, decimal value) =>
        value >= int.MinValue
        && value <= int.MaxValue
        && NewsScoringRules.Bands[type].Any(b => b.Contains((int)value));

    private static string OutOfRangeMessage(MeasurementType type)
    {
        var bands = NewsScoringRules.Bands[type];
        var min = bands.Min(b => b.LowerExclusive) + 1;
        var max = bands.Max(b => b.UpperInclusive);

        return $"{type} must be between {min} and {max}.";
    }
}
