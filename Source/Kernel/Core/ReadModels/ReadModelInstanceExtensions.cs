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
    /// Excludes initialization state unless declared in the read model schema.
    /// </summary>
    /// <param name="instance">The sink-owned instance.</param>
    /// <param name="schema">The read model schema.</param>
    /// <returns>The response state, without mutating the sink-owned instance.</returns>
    public static ExpandoObject WithoutInitializationState(this ExpandoObject instance, JsonSchema schema)
    {
        if (!((IDictionary<string, object?>)instance).ContainsKey(WellKnownProperties.ReadModelInstanceInitialized) ||
            schema.GetFlattenedProperties().Any(property => property.Name == WellKnownProperties.ReadModelInstanceInitialized))
        {
            return instance;
        }

        var result = new ExpandoObject();
        var values = (IDictionary<string, object?>)result;
        foreach (var (name, value) in instance.Where(property => property.Key != WellKnownProperties.ReadModelInstanceInitialized))
        {
            values[name] = value;
        }

        return result;
    }
}
