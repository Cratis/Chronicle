// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="IHandleEventsForObserver"/> job step.
/// </summary>
public class HandleEventsForObserverState : JobStepState
{
    /// <summary>
    /// The <see cref="ObserverKey"/> for the observer.
    /// </summary>
    public ObserverKey ObserverKey { get; set; } = ObserverKey.NotSet;

    /// <summary>
    /// Gets or sets the last successfully handled event sequence number.
    /// </summary>
    public EventSequenceNumber LastSuccessfullyHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceNumber"/> to start processing from.
    /// </summary>
    public EventSequenceNumber StartEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceNumber"/> to stop processing at.
    /// </summary>
    public EventSequenceNumber EndEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the <see cref="EventObservationState"/>.
    /// </summary>
    public EventObservationState EventObservationState { get; set; } = EventObservationState.None;

    /// <summary>
    /// Gets or sets the collection of <see cref="EventType"/> to process.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = [];
}

