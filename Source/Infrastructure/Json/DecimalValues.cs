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
}
