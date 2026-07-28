// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_unsubscribing;

public class the_only_subscriber_for_an_event_type : given.a_seeded_router
{
    ObserverKey _observer;
    EventTypeId _eventType;
    int _queueIndex;
    IReadOnlyList<int> _queuesBeforeUnsubscribe;
    IReadOnlyList<int> _queuesAfterUnsubscribe;

    void Establish()
    {
        _observer = ObserverKeyFor("an-observer");
        _eventType = "Some event type";
        _queueIndex = _router.Subscribe(_observer, [_eventType]);
        _queuesBeforeUnsubscribe = _router.GetQueuesToDeliverTo([_eventType]);
    }

    void Because()
    {
        _router.Unsubscribe(_queueIndex, _observer);
        _queuesAfterUnsubscribe = _router.GetQueuesToDeliverTo([_eventType]);
    }

    [Fact] void should_route_the_matching_batch_before_unsubscribing() => _queuesBeforeUnsubscribe.ShouldContain(_queueIndex);

    [Fact] void should_not_route_the_matching_batch_after_unsubscribing() => _queuesAfterUnsubscribe.ShouldNotContain(_queueIndex);
}
