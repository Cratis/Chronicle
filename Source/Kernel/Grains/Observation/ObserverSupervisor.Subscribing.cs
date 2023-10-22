// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the subscribing aspect.
/// </summary>
public partial class ObserverSupervisor
{
    /// <inheritdoc/>
    public Task Subscribe<TObserverSubscriber>(
        ObserverName name,
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? state = null)
        where TObserverSubscriber : IObserverSubscriber
        => Subscribe(name, type, typeof(TObserverSubscriber), eventTypes, siloAddress, state);

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
        ObserverName name,
        ObserverType type,
        Type subscriberType,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? state = null)
    {
        _logger.Subscribing(name, type, _observerId, subscriberType, _microserviceId, _eventSequenceId, _tenantId);
        _failedPartitionSupervisor = new(_observerId, _observerKey, State.Name, eventTypes, State.FailedPartitions, GrainFactory);
        CurrentSubscription = new(_observerId, _observerKey, eventTypes, subscriberType, siloAddress, state);
        await ReadStateAsync();
        State.Name = name;
        State.Type = type;

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

        _logger.GettingTailSequenceNumberForEventTypes(_observerId, _microserviceId, _eventSequenceId, _tenantId, eventTypes);
        var lastSequenceNumber = await EventSequence.GetTailSequenceNumberForEventTypes(eventTypes);
        if (HasDefinitionChanged(eventTypes) && lastSequenceNumber != EventSequenceNumber.Unavailable)
        {
            _logger.DefinitionChanged(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            State.EventTypes = eventTypes;
            await WriteStateAsync();
            await Rewind();
            return;
        }

        State.RunningState = ObserverRunningState.Subscribing;
        State.EventTypes = eventTypes;

        _logger.TailSequenceNumber(_observerId, _microserviceId, _eventSequenceId, _tenantId, State.TailEventSequenceNumber);
        var tailSequenceNumber = State.TailEventSequenceNumber;
        var nextSequenceNumber = lastSequenceNumber.Next();

        if (lastSequenceNumber != EventSequenceNumber.Unavailable &&
            lastSequenceNumber < tailSequenceNumber &&
            State.NextEventSequenceNumber != EventSequenceNumber.Unavailable)
        {
            _logger.NextEventSequenceNumberForEventTypes(
                _observerId,
                _microserviceId,
                _eventSequenceId,
                _tenantId,
                State.NextEventSequenceNumberForEventTypes,
                State.EventTypes);

            if (State.NextEventSequenceNumberForEventTypes == EventSequenceNumber.Unavailable)
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
        }
        else if (State.NextEventSequenceNumber < nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.CatchingUp;
        }
        else
        {
            State.RunningState = ObserverRunningState.Active;
        }

        _logger.WriteState(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        await WriteStateAsync();
        _logger.StateWritten(_observerId, _microserviceId, _eventSequenceId, _tenantId);

        if (State.RunningState == ObserverRunningState.CatchingUp)
        {
            _logger.CatchingUp(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            await StartCatchup();
        }
        else
        {
            _logger.Active(_observerId, _microserviceId, _eventSequenceId, _tenantId);
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
