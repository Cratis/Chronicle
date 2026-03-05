// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.StateMachines;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States.for_Observing.given;

public class an_observing_state : Specification
{
    protected IObserver _observer;
    protected IAppendedEventsQueues _appendedEventsQueues;
    protected Observing _state;
    protected ObserverState _storedState;
    protected ObserverState _resultingStoredState;
    protected TailEventSequenceNumbers _tailEventSequenceNumbers;
    protected ObserverId _observerId;
    protected EventStoreName _eventStoreName;
    protected EventStoreNamespaceName _eventStoreNamespace;
    protected EventSequenceId _eventSequenceId;
    protected ObserverSubscription _subscription;
    protected AppendedEventsQueueSubscription _queueSubscription;
    protected IPersistentState<ObserverDefinition> _definitionState;
    protected ObserverDefinition _observerDefinition;
    protected ObserverKey _observerKey;
    protected IAppendedEventsQueue _appendedEventsQueue;
    protected IEnumerable<EventType> _eventTypes = [
        new EventType(Guid.NewGuid().ToString(), 0),
        new EventType(Guid.NewGuid().ToString(), 1)
    ];

    void Establish()
    {
        _eventStoreName = "some_event_store";
        _eventStoreNamespace = "some_namespace";
        _eventSequenceId = EventSequenceId.Log;

        _observer = Substitute.For<IObserver>();
        _appendedEventsQueues = Substitute.For<IAppendedEventsQueues>();
        _appendedEventsQueue = Substitute.For<IAppendedEventsQueue>();
        _observerId = Guid.NewGuid().ToString();
        _observerKey = new ObserverKey(
            _observerId,
            _eventStoreName,
            _eventStoreNamespace,
            _eventSequenceId);
        _appendedEventsQueues.Subscribe(_observerKey, Arg.Any<IEnumerable<EventType>>()).Returns(_queueSubscription);
        _queueSubscription = new(_observerKey, 0);
        _appendedEventsQueues.Subscribe(Arg.Any<ObserverKey>(), _eventTypes).Returns(_queueSubscription);
        _definitionState = Substitute.For<IPersistentState<ObserverDefinition>>();
        _observerDefinition = new()
        {
            Identifier = _observerId,
            EventTypes = _eventTypes
        };
        _definitionState.State.Returns(_observerDefinition);

        _state = new Observing(
            _appendedEventsQueues,
            _eventStoreName,
            _eventStoreNamespace,
            _eventSequenceId,
            _definitionState,
            Substitute.For<ILogger<Observing>>());
        _state.SetStateMachine(_observer);
        _storedState = new ObserverState
        {
            Identifier = _observerId,
            RunningState = ObserverRunningState.Active,
        };

        _subscription = new ObserverSubscription(
            _observerId,
            new(_observerId, _eventStoreName, _eventStoreNamespace, _eventSequenceId),
            _eventTypes,
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        _observer.GetSubscription().Returns(_ => _subscription);

        _tailEventSequenceNumbers = new TailEventSequenceNumbers(_observerDefinition.EventSequenceId, _subscription.EventTypes.ToImmutableList(), 0, 0);
    }
}
