// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventSequences;
using Aksio.Cratis.Events.Store.Observation;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the subscribing aspect.
/// </summary>
public partial class Observer
{
    /// <inheritdoc/>
    public async Task Subscribe(IEnumerable<EventType> eventTypes, ObserverNamespace observerNamespace)
    {
        _logger.Subscribing(_observerId, _microserviceId, _eventSequenceId, _tenantId);

        State.CurrentNamespace = observerNamespace;

        await TryResumeAnyFailedPartitions();

        State.RunningState = ObserverRunningState.Subscribing;
        await UnsubscribeStream();

        if (HasDefinitionChanged(eventTypes))
        {
            State.EventTypes = eventTypes;
            await WriteStateAsync();
            await Rewind();
            return;
        }

        var nextSequenceNumber = await _eventSequence!.GetNextSequenceNumber();
        if (State.Offset < nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.CatchingUp;
            _logger.CatchingUp(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        }

        if (State.Offset == nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.Active;
            _logger.Active(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        }

        State.EventTypes = eventTypes;
        await WriteStateAsync();

        _streamSubscription = await _stream!.SubscribeAsync(
            HandleEventForPartitionedObserverWhenSubscribing,
            new EventSequenceNumberTokenWithFilter(State.Offset, eventTypes));
    }

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        _logger.Unsubscribing(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        State.CurrentNamespace = ObserverNamespace.NotSet;
        await WriteStateAsync();
        await UnsubscribeStream();
    }

    Task HandleEventForPartitionedObserverWhenSubscribing(AppendedEvent @event, StreamSequenceToken token)
    {
        if (State.IsPartitionFailed(@event.Context.EventSourceId) ||
            State.IsRecoveringPartition(@event.Context.EventSourceId) ||
            State.RunningState == ObserverRunningState.Replaying ||
            !HasSubscribedObserver)
        {
            return Task.CompletedTask;
        }

        return HandleEventForPartitionedObserver(@event, true);
    }
}
