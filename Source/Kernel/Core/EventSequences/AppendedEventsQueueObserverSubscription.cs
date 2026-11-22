// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents a subscription to <see cref="AppendedEventsQueue"/>.
/// </summary>
/// <param name="ObserverKey"><see cref="ObserverKey"/> for the observer.</param>
/// <param name="EventTypes">The shared <see cref="EventTypeSet"/> the observer subscribes to.</param>
/// <param name="Filters">Optional <see cref="ObserverFilters"/> to apply when dispatching events.</param>
/// <param name="AllEventTypes">Whether the observer receives every event, whatever its type - <paramref name="EventTypes"/> is ignored when it does.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.EventSequences.AppendedEventsQueueObserverSubscription")]
public record AppendedEventsQueueObserverSubscription(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] EventTypeSet EventTypes,
    [Id(2)] ObserverFilters? Filters = null,
    [Id(3)] bool AllEventTypes = false)
{
    /// <summary>
    /// Gets the membership of this subscription, which is the precomputed one of the shared <see cref="EventTypeSet"/>.
    /// </summary>
    internal FrozenSet<EventTypeId> EventTypeIdSet => AllEventTypes
        ? []
        : EventTypes.Ids;
}
