// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents a subscription to <see cref="AppendedEventsQueue"/>.
/// </summary>
/// <param name="ObserverKey"><see cref="ObserverKey"/> for the observer.</param>
/// <param name="EventTypeIds">Collection of <see cref="EventTypeId"/> event types.</param>
/// <param name="Filters">Optional <see cref="ObserverFilters"/> to apply when dispatching events.</param>
/// <param name="AllEventTypes">Whether the observer receives every event, whatever its type - <paramref name="EventTypeIds"/> is ignored when it does.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.EventSequences.AppendedEventsQueueObserverSubscription")]
public record AppendedEventsQueueObserverSubscription(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] IEnumerable<EventTypeId> EventTypeIds,
    [Id(2)] ObserverFilters? Filters = null,
    [Id(3)] bool AllEventTypes = false)
{
    /// <summary>
    /// Keeps derived membership out of record equality and lets replaced subscriptions be collected.
    /// </summary>
    static readonly ConditionalWeakTable<AppendedEventsQueueObserverSubscription, FrozenSet<EventTypeId>> _eventTypeIdSets = new();

    /// <summary>
    /// Gets the cached membership for this subscription.
    /// </summary>
    internal FrozenSet<EventTypeId> EventTypeIdSet => AllEventTypes
        ? []
        : _eventTypeIdSets.GetValue(this, static subscription => subscription.EventTypeIds.ToFrozenSet());
}
