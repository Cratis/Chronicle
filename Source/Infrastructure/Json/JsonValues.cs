// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Helpers for comparing individual JSON values.
/// </summary>
public static class JsonValues
{
    /// <summary>
    /// Renders a JSON value as a string that compares equal for any two representations of the same value.
    /// </summary>
    /// <param name="value">The value to render, which may be <see langword="null"/>.</param>
    /// <returns>The canonical representation of the value.</returns>
    /// <remarks>
    /// A number is rendered without the trailing zeros its writer happened to use, so the same value survives a round
    /// trip through a store that renders it at a different scale or width. Everything else is rendered as JSON, which
    /// keeps a string distinct from a number sharing its digits.
    /// </remarks>
    public static string Canonical(JsonNode? value)
    {
        if (value is not JsonValue jsonValue || !jsonValue.TryGetValue<decimal>(out var number))
        {
            return value?.ToJsonString() ?? "null";
        }

        var text = number.ToString(CultureInfo.InvariantCulture);
        return text.Contains('.') ? text.TrimEnd('0').TrimEnd('.') : text;
    }
}
