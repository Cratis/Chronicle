// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using IEventSequence = Cratis.Chronicle.Grains.EventSequences.IEventSequence;

namespace Cratis.Chronicle.Grains.Observation.States.for_Routing.given;

public class a_routing_state : Specification
{
    protected IObserver _observer;
    protected IEventSequence _eventSequence;
    protected IReplayEvaluator _replayEvaluator;
    protected Routing _state;
    protected ObserverState _storedState;
    protected ObserverState _resultingStoredState;
    protected TailEventSequenceNumbers _tailEventSequenceNumbers;
    protected ObserverSubscription _subscription;
    protected ObserverKey _observerKey;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _eventSequence = Substitute.For<IEventSequence>();
        _observerKey = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        _replayEvaluator = Substitute.For<IReplayEvaluator>();
        _state = new Routing(
            _observerKey,
            _replayEvaluator,
            _eventSequence,
            Substitute.For<ILogger<Routing>>());
        _state.SetStateMachine(_observer);
        _storedState = new ObserverState
        {
            RunningState = ObserverRunningState.Routing,
        };

        _subscription = new ObserverSubscription(
            _observerKey.ObserverId,
            _observerKey,
            [],
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        _observer.GetSubscription().Returns(_ => _subscription);

        _tailEventSequenceNumbers = new TailEventSequenceNumbers(_storedState.EventSequenceId, _subscription.EventTypes.ToImmutableList(), 0, 0);

        _eventSequence.GetTailSequenceNumber().Returns(_ => Task.FromResult(_tailEventSequenceNumbers.Tail));
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns(_ => Task.FromResult(_tailEventSequenceNumbers.TailForEventTypes));
    }
}
