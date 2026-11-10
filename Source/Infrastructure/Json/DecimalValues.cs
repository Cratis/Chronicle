// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Converts decimal values without an intermediate binary floating-point conversion.
/// </summary>
public static class DecimalValues
{
    /// <summary>
    /// Converts a legacy double using its shortest round-trip representation.
    /// </summary>
    /// <param name="value">The stored double.</param>
    /// <returns>The decimal represented by the round-trip text.</returns>
    public static decimal FromDouble(double value) => decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a float using its shortest round-trip representation.
    /// </summary>
    /// <param name="value">The stored float.</param>
    /// <returns>The decimal represented by the round-trip text.</returns>
    public static decimal FromSingle(float value) => decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a double to a decimal the way .NET 10 and earlier did, by rounding to 15 significant digits, on every runtime.
    /// </summary>
    /// <param name="value">The double to convert.</param>
    /// <returns>The decimal rounded to 15 significant digits.</returns>
    /// <exception cref="OverflowException">The value is NaN, infinite or outside the decimal range.</exception>
    /// <remarks>
    /// .NET 11 converts a double to its exact decimal expansion (1.23d becomes 1.229999999999999982236431606m). This keeps the earlier result.
    /// </remarks>
    public static decimal FromDoubleRounded(double value) => double.IsFinite(value)
        ? ParseRounded(value.ToString("G15", CultureInfo.InvariantCulture))
        : throw new OverflowException();

    /// <summary>
    /// Converts a float to a decimal the way .NET 10 and earlier did, by rounding to 7 significant digits, on every runtime.
    /// </summary>
    /// <param name="value">The float to convert.</param>
    /// <returns>The decimal rounded to 7 significant digits.</returns>
    /// <exception cref="OverflowException">The value is NaN, infinite or outside the decimal range.</exception>
    public static decimal FromSingleRounded(float value) => float.IsFinite(value)
        ? ParseRounded(value.ToString("G7", CultureInfo.InvariantCulture))
        : throw new OverflowException();

    /// <summary>
    /// Converts a value to a decimal, using the .NET 10 rounding for double and float and <see cref="Convert.ToDecimal(object?, IFormatProvider?)"/> for everything else.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The decimal value.</returns>
    /// <exception cref="OverflowException">A double or float is NaN, infinite or outside the decimal range.</exception>
    public static decimal ToDecimal(object? value) => value switch
    {
        double doubleValue => FromDoubleRounded(doubleValue),
        float singleValue => FromSingleRounded(singleValue),
        _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Reads a decimal from a JSON number or its string representation.
    /// </summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="result">The decimal value.</param>
    /// <returns>Whether the value represents a decimal.</returns>
    public static bool TryFromLiteral(JsonValue value, out decimal result)
    {
        if (value.TryGetValue<decimal>(out result))
        {
            return true;
        }
        if (value.TryGetValue<double>(out var doubleValue))
        {
            return decimal.TryParse(doubleValue.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
        if (value.TryGetValue<float>(out var singleValue))
        {
            return decimal.TryParse(singleValue.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
        return decimal.TryParse(value.TryGetValue<string>(out var text) ? text : value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    static decimal ParseRounded(string text)
    {
        var result = decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        return result == 0m ? 0m : result;
    }
}
