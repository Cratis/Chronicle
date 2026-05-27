// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents an event that an observer has started handling but not yet acknowledged as completed.
/// </summary>
/// <remarks>
/// In-flight events are recorded before a partition handler is invoked and removed once the handler
/// reports successful processing. Persisting them in a separate storage (outside of the main observer
/// state) allows the observer to recover after a crash that interrupted multi-partition handling —
/// without the risk of silently skipping events whose progress was never written back to the main
/// observer state.
/// </remarks>
public class InFlightEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for this in-flight event entry.
    /// </summary>
    public InFlightEventId Id { get; set; } = InFlightEventId.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="ObserverId"/> the in-flight event belongs to.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets the partition <see cref="Key"/> on which the event is being handled.
    /// </summary>
    public Key Partition { get; set; } = Key.Undefined;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceNumber"/> for the event in flight.
    /// </summary>
    public EventSequenceNumber EventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;
}
