// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Prepares sink metadata for responses without changing stored instances.
/// </summary>
public static class ReadModelInstanceExtensions
{
    /// <summary>
    /// Reads the sink's event watermark, or unavailable when it cannot be interpreted.
    /// </summary>
    /// <param name="instance">The materialized instance.</param>
    /// <returns>The last handled event sequence number.</returns>
    public static EventSequenceNumber GetLastHandledEventSequenceNumber(this ExpandoObject instance)
    {
        var values = (IDictionary<string, object?>)instance;
        if (!values.TryGetValue(WellKnownProperties.LastHandledEventSequenceNumber, out var value) || value is null)
        {
            return EventSequenceNumber.Unavailable;
        }

        try
        {
            return (EventSequenceNumber)Convert.ToUInt64(value);
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        {
            return EventSequenceNumber.Unavailable;
        }
    }

    /// <summary>
    /// Excludes the sink bookkeeping properties the read model schema does not declare.
    /// </summary>
    /// <param name="instance">The sink-owned instance.</param>
    /// <param name="schema">The read model schema.</param>
    /// <returns>The response state, without mutating the sink-owned instance.</returns>
    /// <remarks>
    /// The last handled event sequence number is kept: clients mirror it onto the instance to tell how far it has
    /// been brought up to date. Every other <see cref="WellKnownProperties.All"/> entry is sink bookkeeping and
    /// never part of a response unless the schema declares it.
    /// </remarks>
    public static ExpandoObject WithoutSinkBookkeeping(this ExpandoObject instance, JsonSchema schema)
    {
        var values = (IDictionary<string, object?>)instance;
        var bookkeeping = WellKnownProperties.All
            .Where(name => name != WellKnownProperties.LastHandledEventSequenceNumber && values.ContainsKey(name))
            .ToHashSet(StringComparer.Ordinal);
        if (bookkeeping.Count == 0)
        {
            return instance;
        }

        bookkeeping.ExceptWith(schema.GetFlattenedProperties().Select(property => property.Name));
        if (bookkeeping.Count == 0)
        {
            return instance;
        }

        var result = new ExpandoObject();
        var target = (IDictionary<string, object?>)result;
        foreach (var (name, value) in instance.Where(property => !bookkeeping.Contains(property.Key)))
        {
            target[name] = value;
        }

        return result;
    }
}
