// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
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
            await WriteStateAsync();
            return;
        }

        await WriteStateAsync();
        await UnsubscribeStream();
        await Replay();
    }

    /// <inheritdoc/>
    public Task RewindPartition(EventSourceId partition) => throw new NotImplementedException();

    async Task Replay()
    {
        if (State.NextEventSequenceNumber > State.LastHandled)
        {
            _logger.OffsetIsAtTail(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            State.RunningState = ObserverRunningState.TailOfReplay;
            await WriteStateAsync();
            await Subscribe(CurrentSubscription.SubscriberType!, State.EventTypes);
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
