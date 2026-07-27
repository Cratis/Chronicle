// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_subscribing;

public class an_observer_to_an_event_type_not_previously_in_the_union : given.a_seeded_router
{
    ObserverKey _observer;
    EventTypeId _eventType;
    int _queueIndex;
    IReadOnlyList<int> _queuesBeforeSubscribe;
    IReadOnlyList<int> _queuesAfterSubscribe;

    void Establish()
    {
        _observer = ObserverKeyFor("an-observer");
        _eventType = "Some event type";
        _queueIndex = _router.GetQueueIndexFor(_observer);
        _queuesBeforeSubscribe = _router.GetQueuesToDeliverTo([_eventType]);
    }

    void Because()
    {
        _router.Subscribe(_observer, [_eventType]);
        _queuesAfterSubscribe = _router.GetQueuesToDeliverTo([_eventType]);
    }

    [Fact] void should_not_route_the_matching_batch_before_the_subscription() => _queuesBeforeSubscribe.ShouldNotContain(_queueIndex);

    [Fact] void should_route_the_matching_batch_to_the_subscribed_queue_after() => _queuesAfterSubscribe.ShouldContain(_queueIndex);

    [Fact] void should_route_the_matching_batch_only_to_the_subscribed_queue() => _queuesAfterSubscribe.Count.ShouldEqual(1);
}
