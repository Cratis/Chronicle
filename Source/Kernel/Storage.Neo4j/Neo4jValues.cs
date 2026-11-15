// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Converts the values of a graph between what Chronicle works with and what the Neo4j driver sends and returns.
/// </summary>
/// <remarks>
/// Neo4j has no type for a decimal, so a <see cref="decimal"/> is stored as a <see cref="double"/>, which is what Cratis
/// Arc does when it reads the graph.
/// </remarks>
static class Neo4jValues
{
    /// <summary>
    /// Convert a value to what the driver sends.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value for the driver.</returns>
    /// <exception cref="UnsupportedGraphValue">Thrown when the value is not a primitive or a list of primitives.</exception>
    internal static object? ToDriver(object? value) => GraphValues.Normalize(value) switch
    {
        decimal number => (double)number,
        IList list => list.Cast<object?>().Select(ToDriver).ToList(),
        var other => other
    };

    /// <summary>
    /// Convert the properties of an element to what the driver sends.
    /// </summary>
    /// <param name="properties">The properties.</param>
    /// <returns>The properties for the driver.</returns>
    internal static Dictionary<string, object?> ToDriver(IReadOnlyDictionary<string, object?> properties)
    {
        var converted = new Dictionary<string, object?>(properties.Count);
        foreach (var (name, value) in properties)
        {
            GraphBookkeeping.EnsureNotReserved(name);
            converted[name] = ToDriver(value);
        }

        return converted;
    }

    /// <summary>
    /// Convert a value the driver returned to what Chronicle works with.
    /// </summary>
    /// <param name="value">The value from the driver.</param>
    /// <returns>The converted value.</returns>
    internal static object? FromDriver(object? value) => value switch
    {
        ZonedDateTime zoned => zoned.ToDateTimeOffset(),
        IList<object?> list => list.Select(FromDriver).ToList(),
        _ => value
    };

    /// <summary>
    /// Convert the properties of an element the driver returned, leaving out the bookkeeping.
    /// </summary>
    /// <param name="properties">The properties from the driver.</param>
    /// <returns>The properties of the model.</returns>
    internal static Dictionary<string, object?> FromDriver(IReadOnlyDictionary<string, object> properties) =>
        properties
            .Where(_ => !_.Key.StartsWith(GraphBookkeeping.Prefix[0]))
            .ToDictionary(_ => _.Key, _ => FromDriver(_.Value));
}
