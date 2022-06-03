// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;

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

        if (_eventSequenceId == EventSequenceId.Outbox && _tenantId == Execution.TenantId.Development)
        {
            Console.WriteLine("Out to the box");
        }

        var lastSequenceNumber = await EventSequenceStorageProvider.GetTailSequenceNumber(State.EventSequenceId, State.EventTypes);

        var nextSequenceNumber = lastSequenceNumber + 1;
        if (State.NextEventSequenceNumber < nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.CatchingUp;
            _logger.CatchingUp(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        }

        if (lastSequenceNumber == EventSequenceNumber.Unavailable || State.NextEventSequenceNumber == nextSequenceNumber)
        {
            State.RunningState = ObserverRunningState.Active;
            _logger.Active(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        }

        await WriteStateAsync();
        await SubscribeStream(HandleEventForPartitionedObserverWhenSubscribing);
    }

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        _logger.Unsubscribing(_observerId, _microserviceId, _eventSequenceId, _tenantId);
        State.CurrentNamespace = ObserverNamespace.NotSet;
        State.RunningState = ObserverRunningState.Disconnected;
        await WriteStateAsync();
        await UnsubscribeStream();
    }

    Task HandleEventForPartitionedObserverWhenSubscribing(AppendedEvent @event)
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
