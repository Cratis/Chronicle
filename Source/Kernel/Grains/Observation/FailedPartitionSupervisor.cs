// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

public class FailedPartitionSupervisor : IChildStateProvider<FailedPartitionsState>
{
    readonly ObserverId _observerId;
    readonly ObserverKey _key;
    readonly IEnumerable<EventType> _eventTypes;
    readonly IGrainFactory _grainFactory;
    readonly List<FailedPartition> _failedPartitions;

    public FailedPartitionSupervisor(ObserverId observerId, ObserverKey key, IEnumerable<EventType> eventTypes, IEnumerable<FailedPartition>? failedPartitions, IGrainFactory grainFactory)
    {
        _observerId = observerId;
        _key = key;
        _eventTypes = eventTypes;
        _grainFactory = grainFactory;
        _failedPartitions = failedPartitions?.ToList() ?? new List<FailedPartition>();
        _grainFactory = grainFactory;
    }

    public async Task Fail(EventSourceId partitionId, EventSequenceNumber sequenceNumber, DateTimeOffset occurred)
    {
        if (_failedPartitions.Any(_ => _.Partition == partitionId))
            return;
        await StartRecovery(partitionId, sequenceNumber, occurred);
    }
    
    public async Task AssessRecovery(EventSourceId partitionId, EventSequenceNumber recoveredTo, DateTimeOffset occurred)
    {
        var failedPartition = _failedPartitions.FirstOrDefault(_ => _.Partition == partitionId);
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
    /// Indicates whether or not the supervisor is currently supervising any failed partitions
    /// </summary>
    public bool HasFailedPartitions => _failedPartitions.Any();

    public bool EventBelongsToFailingPartition(EventSourceId eventSourceId, EventSequenceNumber eventSequenceNumber, DateTimeOffset dateTimeOffset)
    {
        var failedPartition = _failedPartitions.FirstOrDefault(_ => _.Partition == eventSourceId);
        if (failedPartition is null)
            return false;
        failedPartition.Head = eventSequenceNumber;
        return true;
    }
    
    public FailedPartitionsState GetState() => new () { FailedPartitions = _failedPartitions.ToArray() };
    
    async Task StartRecovery(EventSourceId partitionId, EventSequenceNumber sequenceNumber, DateTimeOffset occurred)
    {
        _failedPartitions.Add(new FailedPartition(partitionId, sequenceNumber, occurred));
        await GetRecoveryGrain(partitionId).Recover(sequenceNumber, _eventTypes, _key);
    }

    IRecoverFailedPartition GetRecoveryGrain(EventSourceId partitionId) =>
        _grainFactory.GetGrain<IRecoverFailedPartition>(_observerId,
            PartitionedObserverKey.FromObserverKey(_key, partitionId));
}