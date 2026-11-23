// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>Holds the observer's stable inputs while preparing a partition run.</summary>
/// <param name="NextEventSequenceNumber">The delivery floor, including a page's captured floor when present.</param>
/// <param name="SubscribesToAllEvents">Whether event-type membership is unrestricted.</param>
/// <param name="EventTypeIds">The cached subscribed event type identifiers.</param>
/// <param name="Filters">The optional run-wide observer filters.</param>
internal readonly record struct ObserverRunPreparationContext(
    EventSequenceNumber NextEventSequenceNumber,
    bool SubscribesToAllEvents,
    IReadOnlySet<EventTypeId> EventTypeIds,
    ObserverFilters? Filters);
