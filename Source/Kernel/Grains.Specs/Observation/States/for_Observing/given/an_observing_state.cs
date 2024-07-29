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

    protected Mock<IAppendedEventsQueues> appended_events_queues;
    protected Observing state;
    protected ObserverState stored_state;
    protected ObserverState resulting_stored_state;
    protected TailEventSequenceNumbers tail_event_sequence_numbers;
    protected ObserverId observer_id;
    protected EventStoreName event_store_name;
    protected EventStoreNamespaceName event_store_namespace;
    protected EventSequenceId event_sequence_id;
    protected ObserverSubscription subscription;
    protected AppendedEventsQueueSubscription queue_subscription;
    protected ObserverKey observer_key;
    protected Mock<IAppendedEventsQueue> appended_events_queue;
    protected IEnumerable<EventType> event_types = [
        new EventType(Guid.NewGuid().ToString(), 0),
        new EventType(Guid.NewGuid().ToString(), 1)
    ];

    void Establish()
    {
        event_store_name = "some_event_store";
        event_store_namespace = "some_namespace";
        event_sequence_id = EventSequenceId.Log;

        observer = new();
        appended_events_queues = new();
        appended_events_queue = new();
        observer_id = Guid.NewGuid().ToString();
        observer_key = new ObserverKey(
            observer_id,
            event_store_name,
            event_store_namespace,
            event_sequence_id);
        appended_events_queues.Setup(_ => _.Subscribe(observer_key, IsAny<IEnumerable<EventType>>())).Returns(Task.FromResult(queue_subscription));
        queue_subscription = new(observer_key, 0);
        appended_events_queues.Setup(_ => _.Subscribe(IsAny<ObserverKey>(), event_types)).Returns(Task.FromResult(queue_subscription));


        state = new Observing(
            appended_events_queues.Object,
            event_store_name,
            event_store_namespace,
            event_sequence_id,
            Mock.Of<ILogger<Observing>>());
        state.SetStateMachine(observer.Object);
        stored_state = new ObserverState
        {
            ObserverId = observer_id,
            EventTypes = event_types,
            RunningState = ObserverRunningState.Active,
        };

        subscription = new ObserverSubscription(
            observer_id,
            new(observer_id, event_store_name, event_store_namespace, event_sequence_id),
            event_types,
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));

        tail_event_sequence_numbers = new TailEventSequenceNumbers(stored_state.EventSequenceId, subscription.EventTypes.ToImmutableList(), 0, 0);
    }
}
