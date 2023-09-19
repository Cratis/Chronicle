// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Subscribing.given;

public class a_subscribing_state : Specification
{
    protected Mock<IObserverSupervisor> observer;
    protected Mock<IEventSequenceStorage> event_sequence_storage;
    protected Mock<IStateMachine<ObserverState>> state_machine;
    protected Subscribing state;
    protected ObserverState stored_state;
    protected ObserverState resulting_stored_state;
    protected TailEventSequenceNumbers tail_event_sequence_numbers;
    protected ObserverSubscription subscription;

    void Establish()
    {
        observer = new();
        event_sequence_storage = new();
        state = new Subscribing(observer.Object, event_sequence_storage.Object);
        state_machine = new();
        state.SetStateMachine(state_machine.Object);
        stored_state = new ObserverState
        {
            RunningState = ObserverRunningState.Subscribing,
        };

        subscription = new ObserverSubscription(
            Guid.NewGuid(),
            new(MicroserviceId.Unspecified, TenantId.Development, EventSequenceId.Log),
            Enumerable.Empty<EventType>(),
            typeof(object),
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));

        tail_event_sequence_numbers = new TailEventSequenceNumbers(stored_state.EventSequenceId, subscription.EventTypes.ToImmutableList(), 0, 0);

        event_sequence_storage
            .Setup(_ => _.GetTailSequenceNumbers(stored_state.EventSequenceId, IsAny<IEnumerable<EventType>>()))
            .Returns(() => Task.FromResult(tail_event_sequence_numbers));
    }
}
