// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Microsoft.Extensions.Logging;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Routing.given;

public class a_routing_state : Specification
{
    protected Mock<IObserver> observer;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<IStateMachine<ObserverState>> state_machine;
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
        observer_key = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        state = new Routing(observer_key, observer.Object, event_sequence.Object, Mock.Of<ILogger<Routing>>());
        state_machine = new();
        state.SetStateMachine(state_machine.Object);
        stored_state = new ObserverState
        {
            RunningState = ObserverRunningState.Routing,
        };

        subscription = new ObserverSubscription(
            Guid.NewGuid(),
            new(MicroserviceId.Unspecified, TenantId.Development, EventSequenceId.Log),
            Enumerable.Empty<EventType>(),
            typeof(object),
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));

        tail_event_sequence_numbers = new TailEventSequenceNumbers(stored_state.EventSequenceId, subscription.EventTypes.ToImmutableList(), 0, 0);

        event_sequence.Setup(_ => _.GetTailSequenceNumber()).Returns(() => Task.FromResult(tail_event_sequence_numbers.Tail));
        event_sequence.Setup(_ => _.GetTailSequenceNumberForEventTypes(IsAny<IEnumerable<EventType>>())).Returns(() => Task.FromResult(tail_event_sequence_numbers.TailForEventTypes));
    }
}
