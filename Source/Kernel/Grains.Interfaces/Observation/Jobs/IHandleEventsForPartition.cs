// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Jobs;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Defines a step in the replay job that handles events for a partition.
/// </summary>
public interface IHandleEventsForPartition : IJobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult, HandleEventsForPartitionState>
{
    /// <summary>
    /// Persists state about a new successfully handled <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The last handled event sequence number.</param>
    /// <returns>The task representing the operation.</returns>
    Task ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber);
}
