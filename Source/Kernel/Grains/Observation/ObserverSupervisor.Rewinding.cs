// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Workers;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the rewinding aspect.
/// </summary>
public partial class ObserverSupervisor
{
    /// <inheritdoc/>
    public async Task Rewind()
    {
        var disconnected = State.IsDisconnected;
        _logger.Rewinding(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        State.RunningState = ObserverRunningState.Rewinding;
        State.NextEventSequenceNumber = EventSequenceNumber.First;

        if (disconnected || !CurrentSubscription.IsSubscribed)
        {
            _logger.IgnoringRewinding(_observerId, _microserviceId, _eventSequenceId, _tenantId, disconnected, CurrentSubscription.IsSubscribed);
            await WriteStateAsync();
            return;
        }

        await WriteStateAsync();
        await UnsubscribeStream();
        await Replay();
    }

    /// <inheritdoc/>
    public async Task<IWorker<ReplayPartitionRequest, ReplayPartitionResponse>> RewindPartitionTo(EventSourceId partition, EventSequenceNumber sequenceNumber)
    {
        var worker = GrainFactory.GetGrain<IWorker<ReplayPartitionRequest, ReplayPartitionResponse>>(Guid.NewGuid());
        await worker.Start(new(
             _observerId,
             _microserviceId,
             _eventSequenceId,
             _tenantId,
             partition,
             sequenceNumber,
             State.EventTypes));

        return worker;
    }

    /// <inheritdoc/>
    public Task<IWorker<ReplayPartitionRequest, ReplayPartitionResponse>> RewindPartition(EventSourceId partition) => RewindPartitionTo(partition, EventSequenceNumber.First);

    async Task Replay()
    {
        if (State.NextEventSequenceNumber > State.LastHandled)
        {
            _logger.OffsetIsAtTail(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            State.RunningState = ObserverRunningState.TailOfReplay;
            await WriteStateAsync();
            await Subscribe(State.Name, State.Type, CurrentSubscription.SubscriberType!, State.EventTypes, CurrentSubscription.SiloAddress);
            return;
        }

        if (State.HasFailedPartitions)
        {
            _logger.ClearingFailedPartitions(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            if (_failedPartitionSupervisor is not null)
            {
                await _failedPartitionSupervisor.Reset();
                State.FailedPartitions = _failedPartitionSupervisor.GetState().FailedPartitions;
            }
        }

        _logger.Replaying(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        State.RunningState = ObserverRunningState.Replaying;
        await StartReplay();
        await WriteStateAsync();
    }
}
