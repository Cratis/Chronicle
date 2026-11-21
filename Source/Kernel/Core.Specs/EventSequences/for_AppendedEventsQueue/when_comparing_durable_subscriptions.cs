// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue;

public class when_comparing_durable_subscriptions : given.all_dependencies
{
    const string EventStore = "store";
    const string Namespace = "namespace";
    static readonly EventSequenceId _eventSequence = EventSequenceId.Log;

    AppendedEventsQueue _queue;
    AppendedEventsQueueDurableSubscriptionDisagreements _result;
    ObserverId _inBoth;
    ObserverId _storedOnly;
    ObserverId _liveOnly;

    async Task Establish()
    {
        var options = new ChronicleOptions();
        var router = new AppendedEventsQueueRouter(options.Events.Queues);
        ObserverId IdInQueue(int queue, string name) => Enumerable.Range(0, 1000).Select(index => $"{name}-{index}").First(id => router.GetQueueIndexFor(Key(id)) == queue);

        _inBoth = IdInQueue(0, "both");
        _storedOnly = IdInQueue(0, "stored-only");
        _liveOnly = IdInQueue(0, "live-only");
        var storedForAnotherQueue = IdInQueue(1, "other-queue");

        var storage = Substitute.For<IStorage>();
        var observers = Substitute.For<IObserverStateStorage>();
        storage.GetEventStore(EventStore).GetNamespace(Namespace).Observers.Returns(observers);
        var subscription = new ObserverDurableSubscription(ObserverSubscriptionKind.Projection, true, null, []);
        observers.GetDurableSubscriptions(_eventSequence).Returns(new Dictionary<ObserverId, ObserverDurableSubscription>
        {
            [_inBoth] = subscription,
            [_storedOnly] = subscription,
            [storedForAnotherQueue] = subscription
        });

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Options.Create(options),
            Substitute.For<ILogger<AppendedEventsQueue>>(),
            storage: storage);
        _queue.Identify(new EventSequenceKey(_eventSequence, EventStore, Namespace), 0);
        var eventTypes = new[] { new EventType("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First) };
        await _queue.Subscribe(Key(_inBoth), eventTypes);
        await _queue.Subscribe(Key(_liveOnly), eventTypes);
    }

    async Task Because() => _result = await _queue.CompareDurableSubscriptions();

    [Fact] void should_report_the_stored_subscription_that_is_not_live() => _result.StoredNotLive.Single().ShouldEqual(_storedOnly);
    [Fact] void should_report_the_live_subscription_that_is_not_stored() => _result.LiveNotStored.Single().ShouldEqual(_liveOnly);
    [Fact] void should_not_report_a_subscription_that_is_both_stored_and_live() => _result.StoredNotLive.ShouldNotContain(_inBoth);
    [Fact] void should_not_report_the_stored_subscription_of_an_observer_in_another_queue() => _result.StoredNotLive.Count.ShouldEqual(1);

    static ObserverKey Key(ObserverId id) => new(id, EventStore, Namespace, _eventSequence);
}
