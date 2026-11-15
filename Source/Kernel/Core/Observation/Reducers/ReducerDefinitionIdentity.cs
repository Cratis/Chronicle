// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Determines whether two <see cref="ReducerDefinition"/> instances are identical in every field.
/// </summary>
public static class ReducerDefinitionIdentity
{
    /// <summary>
    /// Checks whether the incoming definition is identical to the stored one, in which case storing it again changes nothing.
    /// </summary>
    /// <param name="stored">The stored <see cref="ReducerDefinition"/>.</param>
    /// <param name="incoming">The incoming <see cref="ReducerDefinition"/>.</param>
    /// <returns>True when every field is identical; false otherwise.</returns>
    public static bool IsIdentical(ReducerDefinition stored, ReducerDefinition incoming) =>
        stored.Identifier == incoming.Identifier &&
        stored.EventSequenceId == incoming.EventSequenceId &&
        stored.ReadModel == incoming.ReadModel &&
        stored.IsActive == incoming.IsActive &&
        stored.Hash == incoming.Hash &&
        stored.GenerationDelivery == incoming.GenerationDelivery &&
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
