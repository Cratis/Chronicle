// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;

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
        State.NextEventSequenceNumber = EventSequenceNumber.First;

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
        if (State.NextEventSequenceNumber > State.LastHandled)
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

        var headSequenceNumber = await EventSequenceStorageProvider.GetHeadSequenceNumber(State.EventTypes);
        await SubscribeStream(_ => HandleEventForPartitionedObserverWhenReplaying(_, headSequenceNumber));
    }

    async Task HandleEventForPartitionedObserverWhenReplaying(AppendedEvent @event, EventSequenceNumber headSequenceNumber)
    {
        var state = EventObservationState.Replay;

        if (headSequenceNumber == @event.Metadata.SequenceNumber)
        {
            state |= EventObservationState.HeadOfReplay;
        }

        if (@event.Metadata.SequenceNumber == State.LastHandled)
        {
            state |= EventObservationState.TailOfReplay;
        }

        @event = new(@event.Metadata, @event.Context.WithState(state), @event.Content);

        await HandleEventForPartitionedObserver(@event);

        if (state.HasFlag(EventObservationState.TailOfReplay))
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
