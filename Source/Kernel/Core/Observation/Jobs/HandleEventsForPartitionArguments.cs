// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the arguments passed along to a job step representing a specific key on an observer.
/// </summary>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> with extended details about the observer.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="Partition">The partition in the form a <see cref="Key"/>.</param>
/// <param name="StartEventSequenceNumber">The event sequence number the job step should start from.</param>
/// <param name="EndEventSequenceNumber">The event sequence number the job step should go to.</param>
/// <param name="EventObservationState">The event observation state to set for the events.</param>
/// <param name="EventTypes">The event types that are to replay.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.Jobs.HandleEventsForPartitionArguments")]
public record HandleEventsForPartitionArguments(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] ObserverType ObserverType,
    [Id(2)] Key Partition,
    [Id(3)] EventSequenceNumber StartEventSequenceNumber,
    [Id(4)] EventSequenceNumber EndEventSequenceNumber,
    [Id(5)] EventObservationState EventObservationState,
    [Id(6)] IEnumerable<EventType> EventTypes) : ObserverPartitionedJobRequest(ObserverKey, ObserverType, Partition)
{
    /// <summary>
    /// Gets the failure episode that owns the recovery, or null for a legacy job.
    /// </summary>
    [Id(7)]
    public FailedPartitionId? EpisodeId { get; init; }

    /// <summary>
    /// Gets the deferral that owns the recovery, or null when not deferred.
    /// </summary>
    [Id(8)]
    public Guid? DeferralId { get; init; }

    /// <summary>
    /// Gets the replay operation that scheduled the step, or null when it was not scheduled by a replay.
    /// </summary>
    [Id(9)]
    public ReplayEpoch? ReplayEpoch { get; init; }
}
