// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;

namespace Aksio.Cratis.Kernel.Grains.Observation.New.States.for_Subscribing.when_entering;

public class and_it_is_indexing : Specification
{
    Mock<IEventSequenceStorage> event_sequence_storage;
    Mock<IStateMachine<ObserverState>> state_machine;

    Subscribing state;
    ObserverState stored_state;
    ObserverState resulting_stored_state;

    void Establish()
    {
        event_sequence_storage = new();
        state = new Subscribing(event_sequence_storage.Object);
        state_machine = new();
        state.SetStateMachine(state_machine.Object);
        stored_state = new ObserverState
        {
            RunningState = ObserverRunningState.Indexing,
            Subscription = new ObserverSubscription(
                Guid.NewGuid(),
                new(MicroserviceId.Unspecified, TenantId.Development, EventSequenceId.Log),
                Enumerable.Empty<EventType>(),
                typeof(object),
                string.Empty)
        };

        event_sequence_storage
            .Setup(_ => _.GetTailSequenceNumbers(stored_state.EventSequenceId, stored_state.Subscription.EventTypes))
            .Returns(Task.FromResult(new TailEventSequenceNumbers(stored_state.EventSequenceId, stored_state.Subscription.EventTypes.ToImmutableList(), 0, 0)));
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_transition_to_indexing() => state_machine.Verify(_ => _.TransitionTo<Indexing>(), Once());
}
