// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IRetryFailedPartition"/>.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="Key"><see cref="Key">Partition</see> to retry.</param>
/// <param name="FromSequenceNumber">From <see cref="EventSequenceNumber"/> to retry.</param>
/// <param name="EventTypes">The event types to replay.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.Jobs.RetryFailedPartitionRequest")]
public record RetryFailedPartitionRequest(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] ObserverType ObserverType,
    [Id(2)] Key Key,
    [Id(3)] EventSequenceNumber FromSequenceNumber,
    [Id(4)] IEnumerable<EventType> EventTypes) : ObserverPartitionedJobRequest(ObserverKey, ObserverType, Key)
{
    /// <summary>
    /// Gets the failure episode that owns the recovery, or null for a legacy job.
    /// </summary>
    [Id(5)]
    public FailedPartitionId? EpisodeId { get; init; }

    /// <summary>
    /// Gets the deferral that owns the recovery, or null when not deferred.
    /// </summary>
    [Id(6)]
    public Guid? DeferralId { get; init; }

    /// <summary>
    /// Gets the replay operation that produced the failure being recovered, or null.
    /// </summary>
    [Id(7)]
    public ReplayEpoch? ReplayEpoch { get; init; }
}
