// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_routing;

public class a_batch_with_no_matching_subscription : given.a_seeded_router
{
    ObserverKey _observer;
    EventTypeId _subscribedEventType;
    EventTypeId _unrelatedEventType;
    int _queueIndex;
    IReadOnlyList<int> _queues;

    void Establish()
    {
        _observer = ObserverKeyFor("an-observer");
        _subscribedEventType = "Subscribed event type";
        _unrelatedEventType = "Unrelated event type";
        _queueIndex = _router.Subscribe(_observer, [_subscribedEventType]);
    }

    void Because() => _queues = _router.GetQueuesToDeliverTo([_unrelatedEventType]);

    [Fact] void should_exclude_the_seeded_queue_with_no_matching_subscriber() => _queues.ShouldNotContain(_queueIndex);

    [Fact] void should_route_to_no_queue() => _queues.ShouldBeEmpty();
}
