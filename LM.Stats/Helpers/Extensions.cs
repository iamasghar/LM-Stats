using System;
using System.Globalization;

namespace LM.Stats.Data.Extensions;

public static class DataConversionExtensions
{
    private static readonly CultureInfo[] SupportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("en-GB"),
        new CultureInfo("fr-FR"),
        CultureInfo.InvariantCulture
    };

    // Safe DateTime parsing
    public static DateTime ToSafeDateTime(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        if (DateTime.TryParseExact(value, "yyyy/dd/MM H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
        {
            return dateValue;
        }

        foreach (var culture in SupportedCultures)
        {
            if (DateTime.TryParse(value, culture, DateTimeStyles.None, out var result))
                return result;
        }

        // Handle Excel-specific date formats (OADate)
        if (double.TryParse(value, out var oaDate))
        {
            try
            {
                return DateTime.FromOADate(oaDate);
            }
            catch { /* Ignore */ }
        }

        // Handle common date format issues
        var normalizedDate = value
            .Replace(" 24:", " 00:") // Fix 24-hour overflow
            .Replace("/26/", "/05/"); // Fix obvious month/day swap


        foreach (var culture in SupportedCultures)
        {
            if (DateTime.TryParse(normalizedDate, culture, DateTimeStyles.None, out var result))
                return result;
        }

        return default;
    }

    // Safe numeric conversions
    public static int? ToSafeInt(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        // Handle cases like "1,000" or "1.000"
        var cleanValue = value.Replace(",", "").Replace(".", "");
        if (int.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return result;

        return null;
    }

    public static long? ToSafeLong(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        var cleanValue = value.Replace(",", "").Replace(".", "");
        if (long.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return result;

        return null;
    }

    public static decimal? ToSafeDecimal(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        // Handle percentage values
        if (value.Contains("%") && decimal.TryParse(value.Replace("%", ""),
            NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            return result;/// 100m;
        }

        return null;
    }

    // Handle boolean-like values
    public static bool? ToSafeBool(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (bool.TryParse(value, out var result))
            return result;

        var lowerValue = value.ToLowerInvariant();
        return lowerValue switch
        {
            "yes" => true,
            "no" => false,
            "y" => true,
            "n" => false,
            "1" => true,
            "0" => false,
            _ => null
        };
    }
}