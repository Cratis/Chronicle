// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

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
    /// <returns>The last confirmed durable event sequence number.</returns>
    /// <remarks>
    /// The checkpoint is persisted with debouncing (see <see cref="Configuration.Jobs.StepCheckpointBatchInterval"/>):
    /// the handled position advances every call, but persisted state only advances after the buffered writes are
    /// confirmed durable on every silo. Resume re-reads from the last durable checkpoint.
    /// </remarks>
    Task<EventSequenceNumber> ReportNewSuccessfullyHandledEvent(EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Confirm buffered writes before advancing the durable checkpoint.
    /// </summary>
    /// <param name="start">Whether to establish the window baseline before handling a new range.</param>
    /// <returns>The last confirmed durable event sequence number.</returns>
    Task<EventSequenceNumber> FlushCheckpoint(bool start = false);
}
