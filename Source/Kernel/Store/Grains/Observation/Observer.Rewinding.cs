// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store.EventSequences;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the rewinding aspect.
/// </summary>
public partial class Observer
{
    /// <inheritdoc/>
    public async Task Rewind()
    {
        _logger.Rewinding(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        State.RunningState = ObserverRunningState.Rewinding;
        State.Offset = EventSequenceNumber.First;

        if (!HasSubscribedObserver)
        {
            return;
        }

        await WriteStateAsync();
        await UnsubscribeStream();
        await Replay();
    }

    async Task Replay()
    {
        if (State.Offset > State.LastHandled)
        {
            _logger.OffsetIsAtTail(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            State.RunningState = ObserverRunningState.TailOfReplay;
            await WriteStateAsync();
            await Subscribe(State.EventTypes, State.CurrentNamespace);
            return;
        }

        if (State.HasFailedPartitions)
        {
            _logger.ClearingFailedPartitions(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            State.ClearFailedPartitions();
            var reminder = await GetReminder(RecoverReminder);
            if (reminder is not null)
            {
                await UnregisterReminder(reminder);
            }
        }

        if (State.IsRecoveringAnyPartition)
        {
            _logger.ClearingRecoveringPartitions(_observerId, _microserviceId, _eventSequenceId, _tenantId);

            foreach (var recoveringPartition in State.RecoveringPartitions)
            {
                if (_streamSubscriptionsByEventSourceId.ContainsKey(recoveringPartition.EventSourceId))
                {
                    await _streamSubscriptionsByEventSourceId[recoveringPartition.EventSourceId].UnsubscribeAsync();
                }
            }

            State.ClearRecoveringPartitions();
        }

        _logger.Replaying(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        State.RunningState = ObserverRunningState.Replaying;
        await WriteStateAsync();

        await SubscribeStream(HandleEventForPartitionedObserverWhenReplaying);
    }

    async Task HandleEventForPartitionedObserverWhenReplaying(AppendedEvent @event)
    {
        var tail = @event.Metadata.SequenceNumber == State.LastHandled;
        if (!tail)
        {
            @event = new(@event.Metadata, @event.Context.WithState(EventObservationState.Replay), @event.Content);
        }
        else
        {
            @event = new(@event.Metadata, @event.Context.WithState(EventObservationState.TailOfReplay), @event.Content);
        }

        await HandleEventForPartitionedObserver(@event);

        if (tail)
        {
            State.RunningState = ObserverRunningState.TailOfReplay;
            await WriteStateAsync();
            await UnsubscribeStream();
            if (HasSubscribedObserver)
            {
                await Subscribe(State.EventTypes, State.CurrentNamespace);
            }
        }
    }
}
