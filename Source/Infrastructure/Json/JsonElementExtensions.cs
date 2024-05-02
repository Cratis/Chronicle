// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Json;

/// <summary>
/// Represents extension methods for <see cref="JsonElement"/>.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Try to get the value of a <see cref="JsonElement"/> as a native CLR type.
    /// </summary>
    /// <param name="element"><see cref="JsonElement"/> to try to get from.</param>
    /// <param name="value">Out result value.</param>
    /// <returns>True if it can, false if not.</returns>
    public static bool TryGetValue(this JsonElement element, out object? value)
    {
        value = null;
        if (element.ValueKind == JsonValueKind.String)
        {
            var valueAsString = element.GetString();
            if ((valueAsString?.Contains('+') ?? false) &&
                DateTimeOffset.TryParse(valueAsString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetValue))
            {
                value = dateTimeOffsetValue;
            }
            else if (DateTime.TryParse(valueAsString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
            {
                value = dateTimeValue;
            }
            else
            {
                value = valueAsString;
            }
        }
        else if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt16(out var int16Value))
            {
                value = int16Value;
            }
            else if (element.TryGetUInt16(out var uint16Value))
            {
                value = uint16Value;
            }
            else if (element.TryGetInt32(out var int32Value))
            {
                value = int32Value;
            }
            else if (element.TryGetUInt32(out var uint32Value))
            {
                value = uint32Value;
            }
            else if (element.TryGetInt64(out var int64Value))
            {
                value = int64Value;
            }
            else if (element.TryGetUInt64(out var uint64Value))
            {
                value = uint64Value;
            }
            else if (element.TryGetDouble(out var floatValue))
            {
                value = floatValue;
            }
        }

        return value != null;
    }
}
