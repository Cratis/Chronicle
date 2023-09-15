// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.New.States.for_Observing.given;

public class an_observing_state : Specification
{
    protected Mock<IObserverEventHandler> event_handler;
    protected Mock<IStreamProvider> stream_provider;
    protected Mock<IStateMachine<ObserverState>> state_machine;
    protected Mock<IAsyncStream<AppendedEvent>> stream;
    protected Mock<StreamSubscriptionHandle<AppendedEvent>> stream_subscription;
    protected Observing state;
    protected ObserverState stored_state;
    protected ObserverState resulting_stored_state;
    protected TailEventSequenceNumbers tail_event_sequence_numbers;
    protected ObserverId observer_id;
    protected MicroserviceId microservice_id;
    protected TenantId tenant_id;
    protected EventSequenceId event_sequence_id;

    void Establish()
    {
        event_handler = new();
        stream_provider = new();
        stream = new();
        stream_provider.Setup(_ => _.GetStream<AppendedEvent>(It.IsAny<StreamId>())).Returns(stream.Object);
        stream_subscription = new();
        stream.Setup(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<EventSequenceNumberToken>(), null)).ReturnsAsync(stream_subscription.Object);

        observer_id = Guid.NewGuid();
        microservice_id = MicroserviceId.Unspecified;
        tenant_id = TenantId.Development;
        event_sequence_id = EventSequenceId.Log;

        state = new Observing(
            event_handler.Object,
            stream_provider.Object,
            observer_id,
            microservice_id,
            tenant_id,
            event_sequence_id);
        state_machine = new();
        state.SetStateMachine(state_machine.Object);
        stored_state = new ObserverState
        {
            RunningState = ObserverRunningState.Active,
            Subscription = new ObserverSubscription(
                Guid.NewGuid(),
                new(microservice_id, tenant_id, event_sequence_id),
                Enumerable.Empty<EventType>(),
                typeof(object),
                string.Empty)
        };



        tail_event_sequence_numbers = new TailEventSequenceNumbers(stored_state.EventSequenceId, stored_state.Subscription.EventTypes.ToImmutableList(), 0, 0);
    }
}
