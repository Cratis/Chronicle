// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Provides parsing of the poll interval expression of a capture source, such as "30s", "5m", "1h" or "1d".
/// A bare number is interpreted as minutes.
/// </summary>
public static class CapturePollInterval
{
    /// <summary>
    /// The minimum interval a capture can be scheduled at - the floor Orleans reminders support.
    /// </summary>
    public static readonly TimeSpan Minimum = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Try to parse a poll interval expression.
    /// </summary>
    /// <param name="expression">The expression to parse.</param>
    /// <param name="interval">The parsed interval, clamped to <see cref="Minimum"/>.</param>
    /// <returns>True when the expression could be parsed, false when not.</returns>
    public static bool TryParse(string? expression, out TimeSpan interval)
    {
        interval = Minimum;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        var trimmed = expression.Trim();
        var unit = char.ToLowerInvariant(trimmed[^1]);
        var numberPart = char.IsDigit(trimmed[^1]) ? trimmed : trimmed[..^1];

        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            return false;
        }

        TimeSpan? parsed = unit switch
        {
            's' => TimeSpan.FromSeconds(value),
            'm' => TimeSpan.FromMinutes(value),
            'h' => TimeSpan.FromHours(value),
            'd' => TimeSpan.FromDays(value),
            _ when char.IsDigit(unit) => TimeSpan.FromMinutes(value),
            _ => null
        };

        if (parsed is null)
        {
            return false;
        }

        interval = parsed.Value < Minimum ? Minimum : parsed.Value;
        return true;
    }
}
