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
    protected Mock<IObserver> observer;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<IReplayEvaluator> replay_evaluator;
    protected Routing state;
    protected ObserverState stored_state;
    protected ObserverState resulting_stored_state;
    protected TailEventSequenceNumbers tail_event_sequence_numbers;
    protected ObserverSubscription subscription;
    protected ObserverKey observer_key;

    void Establish()
    {
        observer = new();
        event_sequence = new();
        observer_key = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        replay_evaluator = new();
        state = new Routing(
            observer_key,
            replay_evaluator.Object,
            event_sequence.Object,
            Mock.Of<ILogger<Routing>>());
        state.SetStateMachine(observer.Object);
        stored_state = new ObserverState
        {
            RunningState = ObserverRunningState.Routing,
        };

        subscription = new ObserverSubscription(
            observer_key.ObserverId,
            observer_key,
            [],
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));

        tail_event_sequence_numbers = new TailEventSequenceNumbers(stored_state.EventSequenceId, subscription.EventTypes.ToImmutableList(), 0, 0);

        event_sequence.Setup(_ => _.GetTailSequenceNumber()).Returns(() => Task.FromResult(tail_event_sequence_numbers.Tail));
        event_sequence.Setup(_ => _.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>())).Returns(() => Task.FromResult(tail_event_sequence_numbers.TailForEventTypes));
    }
}
