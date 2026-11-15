// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors;

/// <summary>
/// Determines whether two <see cref="ReactorDefinition"/> instances are identical in every field.
/// </summary>
public static class ReactorDefinitionIdentity
{
    /// <summary>
    /// Checks whether the incoming definition is identical to the stored one, in which case storing it again changes nothing.
    /// </summary>
    /// <param name="stored">The stored <see cref="ReactorDefinition"/>.</param>
    /// <param name="incoming">The incoming <see cref="ReactorDefinition"/>.</param>
    /// <returns>True when every field is identical; false otherwise.</returns>
    public static bool IsIdentical(ReactorDefinition stored, ReactorDefinition incoming) =>
        stored.Identifier == incoming.Identifier &&
        stored.Owner == incoming.Owner &&
        stored.EventSequenceId == incoming.EventSequenceId &&
        stored.IsReplayable == incoming.IsReplayable &&
        stored.GenerationDelivery == incoming.GenerationDelivery &&
        stored.StartAtTail == incoming.StartAtTail &&
        SameEventTypes(stored.EventTypes, incoming.EventTypes) &&
        SameTags(stored.Tags, incoming.Tags) &&
        SameFilters(stored.Filters, incoming.Filters);

    static bool SameEventTypes(IEnumerable<EventTypeWithKeyExpression>? stored, IEnumerable<EventTypeWithKeyExpression>? incoming) =>
        (stored ?? []).SequenceEqual(incoming ?? []);

    static bool SameTags(IEnumerable<string>? stored, IEnumerable<string>? incoming) =>
        (stored ?? []).SequenceEqual(incoming ?? []);

    static bool SameFilters(ObserverFilters? stored, ObserverFilters? incoming)
    {
        if (stored is null || incoming is null)
        {
            return stored is null && incoming is null;
        }

        return SameTags(stored.Tags, incoming.Tags) &&
            stored.EventSourceType == incoming.EventSourceType &&
            stored.EventStreamType == incoming.EventStreamType;
    }
}
