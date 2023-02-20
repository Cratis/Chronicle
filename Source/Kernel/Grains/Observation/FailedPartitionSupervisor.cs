// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Supervises failed partitions for an ObserverSupervisor.
/// </summary>
public class FailedPartitionSupervisor : IChildStateProvider<FailedPartitionsState>
{
    readonly ObserverId _observerId;
    readonly ObserverKey _observerKey;
    readonly ObserverName _observerName;
    readonly IEnumerable<EventType> _eventTypes;
    readonly IGrainFactory _grainFactory;
    readonly List<FailedPartition> _failedPartitions;

    /// <summary>
    /// Indicates whether or not the supervisor is currently supervising any failed partitions.
    /// </summary>
    public bool HasFailedPartitions => _failedPartitions.Count != 0;

    /// <summary>
    /// Instantiates an instance of <see cref="FailedPartitionSupervisor"/>.
    /// </summary>
    /// <param name="observerId">The Observer Id.</param>
    /// <param name="observerKey">The Observer Key.</param>
    /// <param name="observerName">The name of the Observer.</param>
    /// <param name="eventTypes">Events that the observer monitors.</param>
    /// <param name="failedPartitions">Existing failed partitions from state.</param>
    /// <param name="grainFactory">Grains factory to create child worker grains.</param>
    public FailedPartitionSupervisor(
        ObserverId observerId,
        ObserverKey observerKey,
        ObserverName observerName,
        IEnumerable<EventType> eventTypes,
        IEnumerable<FailedPartition>? failedPartitions,
        IGrainFactory grainFactory)
    {
        _observerId = observerId;
        _observerKey = observerKey;
        _observerName = observerName;
        _eventTypes = eventTypes;
        _grainFactory = grainFactory;
        _failedPartitions = failedPartitions?.ToList() ?? new List<FailedPartition>();
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public FailedPartitionsState GetState() => new() { FailedPartitions = _failedPartitions.ToArray() };

    /// <summary>
    /// Indicates to the supervisor that a partition has failed.
    /// </summary>
    /// <param name="partitionId">The Partition Id that failed.</param>
    /// <param name="sequenceNumber">The position where the failure occurred.</param>
    /// <param name="exceptionMessages">The exception messages that caused the partition failure.</param>
    /// <param name="exceptionStackTrace">The exception stack trace that caused the partition failure.</param>
    /// <param name="occurred">When the failure occurred.</param>
    /// <returns>A task that starts the recovery.</returns>
    public async Task Fail(
        EventSourceId partitionId,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        DateTimeOffset occurred)
    {
        if (_failedPartitions.Any(_ => _.Partition == partitionId))
            return;
        await StartRecovery(partitionId, sequenceNumber, exceptionMessages, exceptionStackTrace, occurred);
    }

    /// <summary>
    /// Assesses the recovery of a partition given the recovered position and the event stream position.
    /// </summary>
    /// <param name="partitionId">The Partition Id.</param>
    /// <param name="recoveredTo">The position where the recovery worker processed to.</param>
    /// <returns>Task.</returns>
    public async Task AssessRecovery(EventSourceId partitionId, EventSequenceNumber recoveredTo)
    {
        var failedPartition = _failedPartitions.Find(_ => _.Partition == partitionId);
        if (failedPartition is null)
            return;
        if (failedPartition.SetRecoveredTo(recoveredTo))
        {
            _failedPartitions.Remove(failedPartition);
            await GetRecoveryGrain(partitionId).Reset();
        }
        else
        {
            await GetRecoveryGrain(partitionId).Catchup(recoveredTo);
        }
    }

    /// <summary>
    /// Indicates whether or not an event belongs to a failing partition.
    /// </summary>
    /// <param name="eventSourceId">The partition.</param>
    /// <param name="eventSequenceNumber">The event position.</param>
    /// <returns>True if this partition is failing, false otherwise.</returns>
    public bool EventBelongsToFailingPartition(EventSourceId eventSourceId, EventSequenceNumber eventSequenceNumber)
    {
        var failedPartition = _failedPartitions.Find(_ => _.Partition == eventSourceId);
        if (failedPartition is null)
            return false;
        failedPartition.Head = eventSequenceNumber;
        return true;
    }

    /// <summary>
    /// Try to recover any failed partitions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task TryRecoveringAnyFailedPartitions()
    {
        foreach (var failedPartition in _failedPartitions)
        {
            await TryRecoveringPartition(failedPartition.Partition);
        }
    }

    /// <summary>
    /// Try to recover a specific partition.
    /// </summary>
    /// <param name="partition">Partition to recover.</param>
    /// <returns>Awaitable task.</returns>
    public async Task TryRecoveringPartition(EventSourceId partition)
    {
        var failedPartition = _failedPartitions.Find(_ => _.Partition == partition);
        if (failedPartition is null) return;

        await GetRecoveryGrain(failedPartition.Partition)
            .Recover(
                _observerKey,
                _observerName,
                failedPartition.RecoveredTo ?? failedPartition.Tail,
                _eventTypes,
                failedPartition.Messages,
                failedPartition.StackTrace);
    }

    /// <summary>
    /// Reset any recovering partitions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task Reset()
    {
        foreach (var failedPartition in _failedPartitions)
        {
            await GetRecoveryGrain(failedPartition.Partition).Reset();
        }
        _failedPartitions.Clear();
    }

    async Task StartRecovery(
        EventSourceId partitionId,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> messages,
        string stackTrace,
        DateTimeOffset occurred)
    {
        _failedPartitions.Add(new FailedPartition(partitionId, sequenceNumber, messages, stackTrace, occurred));
        await GetRecoveryGrain(partitionId)
            .Recover(
                _observerKey,
                _observerName,
                sequenceNumber,
                _eventTypes,
                messages,
                stackTrace);
    }

    IRecoverFailedPartition GetRecoveryGrain(EventSourceId partitionId) => _grainFactory.GetGrain<IRecoverFailedPartition>(_observerId, PartitionedObserverKey.FromObserverKey(_observerKey, partitionId));
}
