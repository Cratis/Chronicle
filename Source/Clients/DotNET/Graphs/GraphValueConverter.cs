// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Converts a CLR value into what a graph stores, so a query clause compares like with like.
/// </summary>
/// <remarks>
/// A graph stores primitives only. A concept is converted as the value it wraps, a <see cref="Guid"/> becomes its string
/// form, an enum its name, a <see cref="decimal"/> a <see cref="double"/>, whole numbers a <see cref="long"/> and a
/// <see cref="DateTime"/> a <see cref="DateTimeOffset"/>. A list is converted item by item. These are the same rules the
/// graph sinks and the host packages apply.
/// </remarks>
public static class GraphValueConverter
{
    /// <summary>
    /// Convert a value.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value the graph stores.</returns>
    public static object? ToGraph(object? value) => value switch
    {
        null => null,
        _ when value.IsConcept() => ToGraph(value.GetConceptValue()),
        string or bool or long or double or DateTimeOffset => value,
        Guid guid => guid.ToString("D"),
        Enum enumValue => enumValue.ToString(),
        decimal number => (double)number,
        byte or sbyte or short or ushort or int or uint => Convert.ToInt64(value, CultureInfo.InvariantCulture),
        ulong unsigned when unsigned <= long.MaxValue => (long)unsigned,
        float or Half => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        DateTime dateTime => new DateTimeOffset(dateTime),
        DateOnly or TimeOnly or TimeSpan => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
        IEnumerable list and not IDictionary => list.Cast<object?>().Select(ToGraph).ToList(),
        _ => value
    };
}
