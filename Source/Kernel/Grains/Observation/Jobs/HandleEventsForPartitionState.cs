// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="HandleEventsForPartition"/> job step.
/// </summary>
public class HandleEventsForPartitionState : JobStepState
{
    /// <summary>
    /// Gets or sets the <see cref="ObserverSubscription"/>.
    /// </summary>
    public ObserverSubscription ObserverSubscription { get; set; } = ObserverSubscription.Unsubscribed;

    /// <summary>
    /// Gets or sets the <see cref="Key"/> partition that the events should be processed for.
    /// </summary>
    public Key Partition { get; set; } = Key.Undefined;

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
}
