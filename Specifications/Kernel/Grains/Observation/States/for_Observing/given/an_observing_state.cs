// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Chronicle.Grains.Observation.States.for_Observing.given;

public class an_observing_state : Specification
{
    protected Mock<IObserver> observer;
    protected Mock<IStreamProvider> stream_provider;
    protected Mock<IAsyncStream<AppendedEvent>> stream;
    protected Mock<StreamSubscriptionHandle<AppendedEvent>> stream_subscription;
    protected Observing state;
    protected ObserverState stored_state;
    protected ObserverState resulting_stored_state;
    protected TailEventSequenceNumbers tail_event_sequence_numbers;
    protected ObserverId observer_id;
    protected EventStoreName event_store_name;
    protected EventStoreNamespaceName event_store_namespace;
    protected EventSequenceId event_sequence_id;
    protected IAsyncObserver<AppendedEvent> observed_stream;
    protected ObserverSubscription subscription;

    void Establish()
    {
        observer = new();
        stream_provider = new();
        stream = new();
        stream_provider.Setup(_ => _.GetStream<AppendedEvent>(IsAny<StreamId>())).Returns(stream.Object);
        stream_subscription = new();
        stream.Setup(_ => _
            .SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), IsAny<EventSequenceNumberToken>(), null))
            .ReturnsAsync((IAsyncObserver<AppendedEvent> o, EventSequenceNumberToken _, string __) =>
            {
                observed_stream = o;
                return stream_subscription.Object;
            });

        observer_id = Guid.NewGuid();
        event_store_name = "some_event_store";
        event_store_namespace = "some_namespace";
        event_sequence_id = EventSequenceId.Log;

        state = new Observing(
            stream_provider.Object,
            event_store_name,
            event_store_namespace,
            event_sequence_id,
            Mock.Of<ILogger<Observing>>());
        state.SetStateMachine(observer.Object);
        stored_state = new ObserverState
        {
            RunningState = ObserverRunningState.Active,
        };

        subscription = new ObserverSubscription(
            Guid.NewGuid(),
            new(event_store_name, event_store_namespace, event_sequence_id),
            Enumerable.Empty<EventType>(),
            typeof(object),
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));

        tail_event_sequence_numbers = new TailEventSequenceNumbers(stored_state.EventSequenceId, subscription.EventTypes.ToImmutableList(), 0, 0);
    }
}
