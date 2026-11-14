// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents one hydrated item of a view as an executor returns it: a neutral row tree keyed by member name.
/// </summary>
/// <param name="Values">The values by member name. A property or an edge property holds what the graph stores
/// (a string, a whole number, a decimal number, a boolean, a timestamp, a list of those) or null when absent. A relation
/// of cardinality many holds an <see cref="IReadOnlyList{T}"/> of <see cref="GraphRow"/>, empty when there is no related
/// node; a relation of cardinality one holds a <see cref="GraphRow"/> or null.</param>
public sealed record GraphRow(IReadOnlyDictionary<string, object?> Values)
{
    /// <summary>
    /// Get the value of a member.
    /// </summary>
    /// <param name="member">The name of the member.</param>
    /// <returns>The value, or null when the row has no value for it.</returns>
    public object? this[string member] => Values.TryGetValue(member, out var value) ? value : null;
}
