// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_routing;

public class a_batch_to_a_queue_with_an_observer_of_all_event_types : given.a_seeded_router
{
    ObserverKey _observer;
    int _queueIndex;
    IReadOnlyList<int> _queues;

    void Establish()
    {
        _observer = ObserverKeyFor("an-observer-of-everything");
        _queueIndex = _router.SubscribeToAllEventTypes(_observer);
    }

    void Because() => _queues = _router.GetQueuesToDeliverTo([new EventTypeId("An event type nobody named")]);

    [Fact] void should_route_to_the_queue_of_the_observer() => _queues.ShouldContain(_queueIndex);
}
