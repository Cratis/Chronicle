// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Provides the normalization of the values that go into a graph: keys, identities and properties.
/// </summary>
/// <remarks>
/// A graph stores primitives and arrays of primitives only. Values a graph engine has no primitive for, such as
/// a <see cref="Guid"/>, travel as their text form, and whole numbers as <see cref="long"/>.
/// </remarks>
public static class GraphValues
{
    /// <summary>
    /// Normalize a value to something a graph can store.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The normalized value: null, a string, a boolean, a long, a double, a decimal, a <see cref="DateTimeOffset"/> or a list of those.</returns>
    /// <exception cref="UnsupportedGraphValue">Thrown when the value is an object, or a list holding objects, which a graph cannot store.</exception>
    public static object? Normalize(object? value) => value switch
    {
        null => null,
        string or bool or long or double or decimal or DateTimeOffset => value,
        Guid guid => guid.ToString("D"),
        byte or sbyte or short or ushort or int or uint => Convert.ToInt64(value, CultureInfo.InvariantCulture),
        ulong unsigned when unsigned <= long.MaxValue => (long)unsigned,
        ulong unsigned => (decimal)unsigned,
        float or Half => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        DateTime dateTime => new DateTimeOffset(dateTime),
        DateOnly or TimeOnly or TimeSpan or Enum => value.ToString(),
        IDictionary => throw new UnsupportedGraphValue(value),
        IEnumerable list => list.Cast<object?>().Select(item => item is IEnumerable and not string ? throw new UnsupportedGraphValue(item) : Normalize(item)).ToList(),
        _ => throw new UnsupportedGraphValue(value)
    };

    /// <summary>
    /// Gets the text form of a key or identity, used to compare them regardless of how they were typed.
    /// </summary>
    /// <param name="value">The key or identity.</param>
    /// <returns>The text form.</returns>
    public static string TextOf(object? value) => Convert.ToString(Normalize(value), CultureInfo.InvariantCulture) ?? string.Empty;
}
