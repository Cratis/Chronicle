// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Extension methods for shaping pipeline state into what an event target publishes.
/// </summary>
static class PublishableState
{
    /// <summary>
    /// Removes the bookkeeping the projection and reducer pipelines keep on state - initialization and watermark flags,
    /// subject tracking - so it is never published, unless the event schema declares a property of that name.
    /// </summary>
    /// <param name="state">The state to clean.</param>
    /// <param name="schema">The schema of the published event type.</param>
    /// <returns>A copy of the state holding only what is publishable.</returns>
    internal static ExpandoObject WithoutBookkeeping(this ExpandoObject state, JsonSchema schema)
    {
        var declared = schema.GetFlattenedProperties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var clean = new ExpandoObject();
        var target = (IDictionary<string, object?>)clean;
        foreach (var (name, value) in state.Where(property => !WellKnownProperties.All.Contains(property.Key) || declared.Contains(property.Key)))
        {
            target[name] = value;
        }

        return clean;
    }
}
