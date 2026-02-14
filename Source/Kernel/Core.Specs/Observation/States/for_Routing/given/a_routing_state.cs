// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.StateMachines;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using IEventSequence = Cratis.Chronicle.Grains.EventSequences.IEventSequence;

namespace Cratis.Chronicle.Observation.States.for_Routing.given;

public class a_routing_state : Specification
{
    protected IObserver _observer;
    protected IEventSequence _eventSequence;
    protected Routing _state;
    protected ObserverState _storedState;
    protected ObserverState _resultingStoredState;
    protected IPersistentState<ObserverDefinition> _definitionState;
    protected TailEventSequenceNumbers _tailEventSequenceNumbers;
    protected ObserverSubscription _subscription;
    protected ObserverKey _observerKey;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _eventSequence = Substitute.For<IEventSequence>();
        _observerKey = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        _definitionState = Substitute.For<IPersistentState<ObserverDefinition>>();
        _definitionState.State = ObserverDefinition.Empty;

        _state = new Routing(
            _observerKey,
            _definitionState,
            _eventSequence,
            Substitute.For<ILogger<Routing>>());
        _state.SetStateMachine(_observer);
        _storedState = new ObserverState
        {
            RunningState = ObserverRunningState.Unknown,
        };

        _subscription = new ObserverSubscription(
            _observerKey.ObserverId,
            _observerKey,
            [EventType.Unknown],
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        _observer.GetSubscription().Returns(_ => _subscription);

        _tailEventSequenceNumbers = new TailEventSequenceNumbers(EventSequenceId.Log, _subscription.EventTypes.ToImmutableList(), 0, 0);

        _eventSequence.GetTailSequenceNumber().Returns(_ => Task.FromResult(_tailEventSequenceNumbers.Tail));
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
        .Returns(_ => Task.FromResult(Result<EventSequenceNumber, GetSequenceNumberError>.Success(_tailEventSequenceNumbers.TailForEventTypes)));
    }
}
