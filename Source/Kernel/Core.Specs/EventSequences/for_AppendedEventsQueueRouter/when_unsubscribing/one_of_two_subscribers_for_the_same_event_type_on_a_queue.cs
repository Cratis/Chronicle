// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_unsubscribing;

public class one_of_two_subscribers_for_the_same_event_type_on_a_queue : given.a_seeded_single_queue_router
{
    ObserverKey _firstObserver;
    ObserverKey _secondObserver;
    EventTypeId _eventType;
    IReadOnlyList<int> _queues;

    void Establish()
    {
        _firstObserver = ObserverKeyFor("first-observer");
        _secondObserver = ObserverKeyFor("second-observer");
        _eventType = "Shared event type";
        _router.Subscribe(_firstObserver, [_eventType]);
        _router.Subscribe(_secondObserver, [_eventType]);
    }

    void Because()
    {
        _router.Unsubscribe(0, _firstObserver);
        _queues = _router.GetQueuesToDeliverTo([_eventType]);
    }

    [Fact] void should_still_route_the_matching_batch_for_the_remaining_subscriber() => _queues.ShouldContain(0);
}
