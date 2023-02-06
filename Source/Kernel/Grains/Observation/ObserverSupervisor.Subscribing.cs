// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the subscribing aspect.
/// </summary>
public partial class ObserverSupervisor
{
    /// <inheritdoc/>
    public Task Subscribe<TObserverSubscriber>(IEnumerable<EventType> eventTypes)
        where TObserverSubscriber : IObserverSubscriber
        => Subscribe(typeof(TObserverSubscriber), eventTypes);

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        SubscriberType = null!;
        _logger.Unsubscribing(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        await StopAnyRunningCatchup();
        State.RunningState = ObserverRunningState.Disconnected;
        await WriteStateAsync();
        await UnsubscribeStream();

        if (_recoverReminder is not null)
        {
            await UnregisterReminder(_recoverReminder);
        }
    }

    async Task Subscribe(Type subscriberType, IEnumerable<EventType> eventTypes)
    {
        _logger.Subscribing(_observerId, subscriberType, _microserviceId, _eventSequenceId, _tenantId);
        SubscriberType = subscriberType;

        if (State.RunningState == ObserverRunningState.Rewinding)
        {
            await Rewind();
            return;
        }

        if (State.RunningState == ObserverRunningState.Replaying)
        {
            await Replay();
            return;
        }

        await TryResumeAnyFailedPartitions();

        await UnsubscribeStream();

        if (HasDefinitionChanged(eventTypes))
        {
            State.EventTypes = eventTypes;
            await WriteStateAsync();
            await Rewind();
            return;
        }

        State.RunningState = ObserverRunningState.Subscribing;
        State.EventTypes = eventTypes;

        var lastSequenceNumber = await EventSequenceStorageProvider.GetTailSequenceNumber(State.EventSequenceId, State.EventTypes);
        var nextSequenceNumber = lastSequenceNumber + 1;

        if (lastSequenceNumber == EventSequenceNumber.Unavailable ||
            State.NextEventSequenceNumber == nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.Active;
            _logger.Active(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        }
        else if (State.NextEventSequenceNumber < nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.CatchingUp;
            _logger.CatchingUp(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        }

        await WriteStateAsync();

        if (State.RunningState == ObserverRunningState.CatchingUp)
        {
            await StartCatchup();
        }
        else
        {
            await SubscribeStream(HandleEventForPartitionedObserverWhenSubscribing);
        }
    }

    Task HandleEventForPartitionedObserverWhenSubscribing(AppendedEvent @event)
    {
        if (State.IsPartitionFailed(@event.Context.EventSourceId) ||
            State.IsRecoveringPartition(@event.Context.EventSourceId) ||
            State.RunningState == ObserverRunningState.Replaying ||
            State.IsDisconnected)
        {
            return Task.CompletedTask;
        }

        return Handle(@event, true);
    }
}
