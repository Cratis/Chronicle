// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Supervises failed partitions for an ObserverSupervisor.
/// </summary>
public class FailedPartitionSupervisor : IChildStateProvider<FailedPartitionsState>
{
    readonly ObserverId _observerId;
    readonly ObserverKey _key;
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
    /// <param name="key">The Observer Key.</param>
    /// <param name="eventTypes">Events that the observer monitors.</param>
    /// <param name="failedPartitions">Existing failed partitions from state.</param>
    /// <param name="grainFactory">Grains factory to create child worker grains.</param>
    public FailedPartitionSupervisor(ObserverId observerId, ObserverKey key, IEnumerable<EventType> eventTypes, IEnumerable<FailedPartition>? failedPartitions, IGrainFactory grainFactory)
    {
        _observerId = observerId;
        _key = key;
        _eventTypes = eventTypes;
        _grainFactory = grainFactory;
        _failedPartitions = failedPartitions?.ToList() ?? new List<FailedPartition>();
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public FailedPartitionsState GetState() => new () { FailedPartitions = _failedPartitions.ToArray() };

    /// <summary>
    /// Indicates to the supervisor that a partition has failed.
    /// </summary>
    /// <param name="partitionId">The Partition Id that failed.</param>
    /// <param name="sequenceNumber">The position where the failure occured.</param>
    /// <param name="occurred">When the failure occurred.</param>
    /// <returns>A task that starts the recovery.</returns>
    public async Task Fail(EventSourceId partitionId, EventSequenceNumber sequenceNumber, DateTimeOffset occurred)
    {
        if (_failedPartitions.Any(_ => _.Partition == partitionId))
            return;
        await StartRecovery(partitionId, sequenceNumber, occurred);
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

    async Task StartRecovery(EventSourceId partitionId, EventSequenceNumber sequenceNumber, DateTimeOffset occurred)
    {
        _failedPartitions.Add(new FailedPartition(partitionId, sequenceNumber, occurred));
        await GetRecoveryGrain(partitionId).Recover(sequenceNumber, _eventTypes, _key);
    }

    IRecoverFailedPartition GetRecoveryGrain(EventSourceId partitionId) => _grainFactory.GetGrain<IRecoverFailedPartition>(_observerId, PartitionedObserverKey.FromObserverKey(_key, partitionId));
}