// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Jobs;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Defines a step in the replay job that handles events for a partition.
/// </summary>
public interface IHandleEventsForPartition : IJobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult>
{
    /// <summary>
    /// Persists state about a new successfully handled <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The last handled event sequence number.</param>
    /// <returns>The task representing the operation.</returns>
    Task ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Gets the persisted state about the last successfully handled <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <returns>The <see cref="Result{TValue, TError}"/> with <see cref="EventSequenceNumber"/> result and <see cref="JobError"/>.</returns>
    [AlwaysInterleave]
    Task<EventSequenceNumber> GetLastSuccessfullyHandledEventSequenceNumber();
}
