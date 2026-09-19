using AssetPulse.Api.Contracts;

namespace AssetPulse.Api.Services;

public static class AssetUpsertValidator
{
    public static bool TryNormalize(
        AssetUpsertRequest? request,
        out NormalizedAssetUpsert? normalized,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        normalized = null;

        var name = NormalizeRequired(request?.Name, "name", 100, errors);
        var assetCode = NormalizeRequired(request?.AssetCode, "assetCode", 32, errors)?.ToUpperInvariant();
        var type = NormalizeRequired(request?.Type, "type", 50, errors);
        var location = NormalizeRequired(request?.Location, "location", 100, errors);
        ValidateMeasurement(request?.Temperature, "temperature", -273.15m, errors);
        ValidateMeasurement(request?.Pressure, "pressure", 0m, errors);

        if (errors.Count > 0)
        {
            return false;
        }

        normalized = new NormalizedAssetUpsert(
            name!,
            assetCode!,
            type!,
            location!,
            request!.Temperature,
            request.Pressure);
        return true;
    }

    private static string? NormalizeRequired(
        string? value,
        string field,
        int maximumLength,
        Dictionary<string, string[]> errors)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            errors[field] = ["This field is required."];
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            errors[field] = [$"This field must be {maximumLength} characters or fewer."];
            return null;
        }

        return normalized;
    }

    private static void ValidateMeasurement(
        decimal? value,
        string field,
        decimal minimum,
        Dictionary<string, string[]> errors)
    {
        if (value is null)
        {
            return;
        }

        if (value < minimum)
        {
            errors[field] = [$"This field must be at least {minimum:0.##}."];
            return;
        }

        if (decimal.Round(value.Value, 2) != value.Value || value is > 99999999.99m)
        {
            errors[field] = ["This field must have at most two decimal places and fit the supported range."];
        }
    }
}
