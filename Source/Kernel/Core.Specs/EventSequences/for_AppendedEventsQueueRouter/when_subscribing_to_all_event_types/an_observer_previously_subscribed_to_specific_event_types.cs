// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_subscribing_to_all_event_types;

public class an_observer_previously_subscribed_to_specific_event_types : given.a_seeded_single_queue_router
{
    ObserverKey _observer;
    int _queueIndex;
    IReadOnlyList<int> _queuesAfterUnsubscribing;
    IReadOnlyList<int> _queuesWhileSubscribed;

    void Establish()
    {
        _observer = ObserverKeyFor("an-observer");
        _router.Subscribe(_observer, [new EventTypeId("Specific event type")]);
    }

    void Because()
    {
        _queueIndex = _router.SubscribeToAllEventTypes(_observer);
        _queuesWhileSubscribed = _router.GetQueuesToDeliverTo([new EventTypeId("Another event type")]);
        _router.Unsubscribe(_queueIndex, _observer);
        _queuesAfterUnsubscribing = _router.GetQueuesToDeliverTo([new EventTypeId("Specific event type")]);
    }

    [Fact] void should_route_every_event_type_to_it() => _queuesWhileSubscribed.ShouldContain(_queueIndex);
    [Fact] void should_not_keep_the_specific_subscription_once_unsubscribed() => _queuesAfterUnsubscribing.ShouldBeEmpty();
}
