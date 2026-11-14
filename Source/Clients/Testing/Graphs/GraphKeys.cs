// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Testing.Graphs;

/// <summary>
/// Compares and matches the values of an in-memory graph the way the graph query semantics define: numbers numerically,
/// everything else by its text form, ordinal.
/// </summary>
internal sealed class GraphKeys : IComparer<object?>
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static GraphKeys Instance { get; } = new();

    /// <summary>
    /// Check whether two stored values are equal.
    /// </summary>
    /// <param name="left">The one value.</param>
    /// <param name="right">The other value.</param>
    /// <returns>True if they are equal, false if not.</returns>
    public static bool AreEqual(object? left, object? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) or (_, null) => false,
        _ when IsNumber(left) && IsNumber(right) => Convert.ToDecimal(left, CultureInfo.InvariantCulture) == Convert.ToDecimal(right, CultureInfo.InvariantCulture),
        _ => Equals(left, right) || string.Equals(GraphValues.TextOf(left), GraphValues.TextOf(right), StringComparison.Ordinal)
    };

    /// <inheritdoc/>
    public int Compare(object? x, object? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        if (IsNumber(x) && IsNumber(y)) return Convert.ToDecimal(x, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(y, CultureInfo.InvariantCulture));
        return string.CompareOrdinal(GraphValues.TextOf(x), GraphValues.TextOf(y));
    }

    static bool IsNumber(object value) => value is long or int or short or byte or double or float or decimal;
}
