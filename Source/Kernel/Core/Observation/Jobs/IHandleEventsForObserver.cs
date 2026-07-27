// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines a step in the replay job that handles events for an observer in event-sequence order.
/// </summary>
public interface IHandleEventsForObserver : IJobStep<HandleEventsForObserverArguments, HandleEventsForPartitionResult, HandleEventsForObserverState>
{
    /// <summary>
    /// Advances the step's progress to a new successfully handled <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The last handled event sequence number.</param>
    /// <returns>The task representing the operation.</returns>
    /// <remarks>
    /// The checkpoint is persisted with debouncing (see <see cref="Configuration.Jobs.StepCheckpointBatchInterval"/>):
    /// the in-memory progress advances every call, but the durable write happens once a batch of checkpoints has
    /// accumulated. Resume re-reads from the last persisted checkpoint and observers are idempotent, so this only
    /// affects how many already-handled events are re-scanned after a crash, never correctness.
    /// </remarks>
    Task ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber);
}

