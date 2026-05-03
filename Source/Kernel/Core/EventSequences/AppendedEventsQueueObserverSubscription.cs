// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents a subscription to <see cref="AppendedEventsQueue"/>.
/// </summary>
/// <param name="ObserverKey"><see cref="ObserverKey"/> for the observer.</param>
/// <param name="EventTypeIds">Collection of <see cref="EventTypeId"/> event types.</param>
/// <param name="Filters">Optional <see cref="ObserverFilters"/> to apply when dispatching events.</param>
public record AppendedEventsQueueObserverSubscription(
    ObserverKey ObserverKey,
    IEnumerable<EventTypeId> EventTypeIds,
    ObserverFilters? Filters = null);
