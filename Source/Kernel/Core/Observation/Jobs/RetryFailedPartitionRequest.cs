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
public record RetryFailedPartitionRequest(
    ObserverKey ObserverKey,
    ObserverType ObserverType,
    Key Key,
    EventSequenceNumber FromSequenceNumber,
    IEnumerable<EventType> EventTypes) : ObserverPartitionedJobRequest(ObserverKey, ObserverType, Key)
{
    /// <summary>
    /// Gets the failure episode that owns the recovery, or null for a legacy job.
    /// </summary>
    public FailedPartitionId? EpisodeId { get; init; }

    /// <summary>
    /// Gets the deferral that owns the recovery, or null when not deferred.
    /// </summary>
    public Guid? DeferralId { get; init; }
}
