// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the arguments passed along to a job step that handles events for an observer in event-sequence order.
/// </summary>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> with extended details about the observer.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="StartEventSequenceNumber">The event sequence number the job step should start from.</param>
/// <param name="EndEventSequenceNumber">The event sequence number the job step should go to.</param>
/// <param name="EventObservationState">The event observation state to set for the events.</param>
/// <param name="EventTypes">The event types that are to replay.</param>
public record HandleEventsForObserverArguments(
    ObserverKey ObserverKey,
    ObserverType ObserverType,
    EventSequenceNumber StartEventSequenceNumber,
    EventSequenceNumber EndEventSequenceNumber,
    EventObservationState EventObservationState,
    IEnumerable<EventType> EventTypes) : IObserverJobRequest
{
    /// <summary>
    /// Gets a value indicating whether events belonging to a partition the observer has already recorded as failed
    /// are skipped rather than delivered.
    /// </summary>
    /// <remarks>
    /// A failed partition is owned by the retry job that is working on it, so delivering its events from here as
    /// well would hand the same events to the subscriber twice at once. Catch-up sets this because it is the path
    /// that runs alongside retries; a replay does not, because it deliberately re-delivers everything.
    /// </remarks>
    public bool SkipFailedPartitions { get; init; }
}

