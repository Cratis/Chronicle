// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Analyzes reducer definition changes that can be evolved without a full replay.
/// </summary>
internal static class ReducerDefinitionEvolution
{
    /// <summary>
    /// Gets added event types when no other part of the reducer definition changed.
    /// </summary>
    /// <param name="previous">The previously registered definition.</param>
    /// <param name="current">The incoming definition.</param>
    /// <returns>The added event types, or an empty collection when the change requires a full replay.</returns>
    public static EventType[] GetAddedEventTypesIfOnlyEventTypesChanged(
        ReducerDefinition previous,
        ReducerDefinition current)
    {
        if (previous.EventSequenceId != current.EventSequenceId ||
            previous.ReadModel != current.ReadModel ||
            previous.IsActive != current.IsActive ||
            previous.Hash != current.Hash ||
            !Equals(previous.Filters, current.Filters) ||
            !previous.Tags.Order().SequenceEqual(current.Tags.Order()))
        {
            return [];
        }

        var previousByType = previous.EventTypes.ToDictionary(_ => _.EventType);
        var currentByType = current.EventTypes.ToDictionary(_ => _.EventType);
        if (previousByType.Keys.Except(currentByType.Keys).Any() ||
            previousByType.Any(entry => currentByType[entry.Key].Key != entry.Value.Key))
        {
            return [];
        }

        return currentByType.Keys.Except(previousByType.Keys).ToArray();
    }
}
