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
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.Jobs.HandleEventsForObserverArguments")]
public record HandleEventsForObserverArguments(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] ObserverType ObserverType,
    [Id(2)] EventSequenceNumber StartEventSequenceNumber,
    [Id(3)] EventSequenceNumber EndEventSequenceNumber,
    [Id(4)] EventObservationState EventObservationState,
    [Id(5)] IEnumerable<EventType> EventTypes) : IObserverJobRequest
{
    /// <inheritdoc/>
    [Id(6)]
    public ObserverReaderScope? ReaderScope { get; init; }

    /// <inheritdoc/>
    [Id(7)]
    public ulong? Revision { get; init; }

    /// <inheritdoc/>
    [Id(8)]
    public ulong? OwnershipEpoch { get; init; }

    /// <summary>
    /// Gets a value indicating whether events belonging to a partition the observer has already recorded as failed
    /// are skipped rather than delivered.
    /// </summary>
    /// <remarks>
    /// A failed partition is owned by the retry job that is working on it, so delivering its events from here as
    /// well would hand the same events to the subscriber twice at once. Catch-up sets this because it is the path
    /// that runs alongside retries; a replay does not, because it deliberately re-delivers everything.
    /// </remarks>
    [Id(9)]
    public bool SkipFailedPartitions { get; init; }

    /// <summary>
    /// Gets the partition suffixes owned by concurrent partition catch-up jobs. This snapshot also excludes their
    /// events if those jobs finish before this step starts or resumes. Empty for legacy steps and replay.
    /// </summary>
    [Id(10)]
    public IReadOnlyList<Concepts.Keys.Key> ExcludedPartitions { get; init; } = [];
}
