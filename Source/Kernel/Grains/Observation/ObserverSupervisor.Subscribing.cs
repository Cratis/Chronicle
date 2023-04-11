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
    public Task Subscribe<TObserverSubscriber>(IEnumerable<EventType> eventTypes, object? subscriberArgs = default)
        where TObserverSubscriber : IObserverSubscriber
        => Subscribe(typeof(TObserverSubscriber), eventTypes, subscriberArgs);

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        CurrentSubscription = ObserverSubscription.Unsubscribed;
        _logger.Unsubscribing(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        await StopAnyRunningCatchup();
        State.RunningState = ObserverRunningState.Disconnected;
        await WriteStateAsync();
        await UnsubscribeStream();
    }

    async Task Subscribe(
        Type subscriberType,
        IEnumerable<EventType> eventTypes,
        object? subscriberArgs = default)
    {
        await ReadStateAsync();

        _failedPartitionSupervisor = new(_observerId, _observerKey, State.Name, eventTypes, State.FailedPartitions, GrainFactory);

        _logger.Subscribing(_observerId, subscriberType, _microserviceId, _eventSequenceId, _tenantId);
        CurrentSubscription = new(_observerId, _observerKey, eventTypes, subscriberType, subscriberArgs!);

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

        TryRecoveringAnyFailedPartitions();

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

        var tailSequenceNumber = await EventSequenceStorageProvider.GetTailSequenceNumber(State.EventSequenceId);
        var lastSequenceNumber = await EventSequenceStorageProvider.GetTailSequenceNumber(State.EventSequenceId, State.EventTypes);
        var nextSequenceNumber = lastSequenceNumber.Next();

        if (lastSequenceNumber != EventSequenceNumber.Unavailable &&
            lastSequenceNumber < tailSequenceNumber &&
            State.NextEventSequenceNumber != EventSequenceNumber.Unavailable)
        {
            var highestNumber = await _eventSequenceStorageProvider().GetNextSequenceNumberGreaterOrEqualThan(
                _eventSequenceId,
                nextSequenceNumber,
                State.EventTypes);

            if (highestNumber == EventSequenceNumber.Unavailable)
            {
                State.RunningState = ObserverRunningState.Active;
                var previousNext = State.NextEventSequenceNumber;
                State.NextEventSequenceNumber = tailSequenceNumber.Next();
                _logger.FastForwarding(
                    previousNext,
                    State.NextEventSequenceNumber,
                    _observerId,
                    _eventSequenceId,
                    _microserviceId,
                    _tenantId);
            }
        }

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
            State.RunningState == ObserverRunningState.Replaying ||
            State.IsDisconnected)
        {
            return Task.CompletedTask;
        }

        return Handle(@event);
    }
}
