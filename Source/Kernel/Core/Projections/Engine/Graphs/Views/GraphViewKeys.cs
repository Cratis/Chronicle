// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Compares and orders the values of graph elements the way graph views define it: numbers numerically, everything else by its
/// text, ordinal, with a Guid by its string form.
/// </summary>
internal sealed class GraphViewKeys : IComparer<object?>
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static GraphViewKeys Instance { get; } = new();

    /// <summary>
    /// Check whether two values are equal.
    /// </summary>
    /// <param name="left">The one value.</param>
    /// <param name="right">The other value.</param>
    /// <returns>True if they are equal, false if not.</returns>
    public static bool AreEqual(object? left, object? right)
    {
        if (left is null || right is null) return left is null && right is null;
        if (TryNumber(left, out var leftNumber) && TryNumber(right, out var rightNumber)) return leftNumber == rightNumber;

        // A value given as text can still be compared with a stored number.
        if ((IsNumber(left) && right is string) || (left is string && IsNumber(right)))
        {
            return string.Equals(GraphValues.TextOf(left), GraphValues.TextOf(right), StringComparison.Ordinal);
        }

        return Equals(left, right) || string.Equals(GraphValues.TextOf(left), GraphValues.TextOf(right), StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public int Compare(object? x, object? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        if (TryNumber(x, out var xNumber) && TryNumber(y, out var yNumber)) return xNumber.CompareTo(yNumber);
        return string.CompareOrdinal(GraphValues.TextOf(x), GraphValues.TextOf(y));
    }

    static bool IsNumber(object value) => value is long or int or short or byte or double or float or decimal;

    static bool TryNumber(object value, out decimal number)
    {
        if (IsNumber(value))
        {
            number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return true;
        }

        number = 0;
        return false;
    }
}
