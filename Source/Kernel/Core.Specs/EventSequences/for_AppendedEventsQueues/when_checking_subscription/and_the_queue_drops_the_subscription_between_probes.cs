// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.when_checking_subscription;

public class and_the_queue_drops_the_subscription_between_probes : given.two_seeded_queues
{
    ObserverKey _observerKey;
    IAppendedEventsQueue _routedQueue;
    IAppendedEventsQueue _otherQueue;
    bool _firstProbe;
    bool _secondProbe;

    void Establish()
    {
        _observerKey = ObserverKeyFor("an-observer-dropped-between-probes");
        _routedQueue = _queueGrains[QueueIndexFor(_observerKey)];
        _otherQueue = _queueGrains.Single(queue => queue != _routedQueue);
        _routedQueue.GetSubscriptions().Returns(
        [
            new AppendedEventsQueueObserverSubscription(_observerKey, [subscribed_event_type]),
            new AppendedEventsQueueObserverSubscription(ObserverKeyFor("another-observer"), [subscribed_event_type])
        ],
        [
            new AppendedEventsQueueObserverSubscription(ObserverKeyFor("another-observer"), [subscribed_event_type])
        ]);
    }

    async Task Because()
    {
        _firstProbe = await _queues.IsSubscribed(_observerKey);
        _secondProbe = await _queues.IsSubscribed(_observerKey);
    }

    [Fact] void should_report_the_first_probe_as_subscribed() => _firstProbe.ShouldBeTrue();
    [Fact] void should_report_the_second_probe_as_not_subscribed() => _secondProbe.ShouldBeFalse();
    [Fact] async Task should_ask_the_routed_queue_for_each_probe() => await _routedQueue.Received(2).GetSubscriptions();
    [Fact] async Task should_not_ask_the_other_queue() => await _otherQueue.DidNotReceive().GetSubscriptions();
}
