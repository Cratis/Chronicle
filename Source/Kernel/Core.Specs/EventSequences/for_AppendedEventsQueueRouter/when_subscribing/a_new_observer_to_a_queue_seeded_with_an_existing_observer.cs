// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_subscribing;

/// <summary>
/// Guards against the reactivation drop: a queue grain can outlive the routing grain, so on reactivation the
/// router seeds from the surviving queue. A later subscription must extend — never replace — that seeded union,
/// otherwise the pre-existing observer would silently stop receiving its events.
/// </summary>
public class a_new_observer_to_a_queue_seeded_with_an_existing_observer : given.a_seeded_single_queue_router
{
    ObserverKey _existingObserver;
    ObserverKey _newObserver;
    EventTypeId _existingEventType;
    EventTypeId _newEventType;
    IReadOnlyList<int> _existingEventTypeQueues;
    IReadOnlyList<int> _newEventTypeQueues;

    void Establish()
    {
        _existingObserver = ObserverKeyFor("existing-observer");
        _newObserver = ObserverKeyFor("new-observer");
        _existingEventType = "Existing event type";
        _newEventType = "New event type";
        _router.Seed(0, [new AppendedEventsQueueObserverSubscription(_existingObserver, [_existingEventType])]);
    }

    void Because()
    {
        _router.Subscribe(_newObserver, [_newEventType]);
        _existingEventTypeQueues = _router.GetQueuesToDeliverTo([_existingEventType]);
        _newEventTypeQueues = _router.GetQueuesToDeliverTo([_newEventType]);
    }

    [Fact] void should_still_route_the_seeded_observers_event_type() => _existingEventTypeQueues.ShouldContain(0);

    [Fact] void should_route_the_newly_subscribed_event_type() => _newEventTypeQueues.ShouldContain(0);
}
