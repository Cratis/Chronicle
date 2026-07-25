// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_routing;

public class a_batch_without_any_seeded_queue : given.a_router
{
    EventTypeId _eventType;
    IReadOnlyList<int> _queues;

    void Establish() => _eventType = "Some event type nobody subscribes to";

    void Because() => _queues = _router.GetQueuesToDeliverTo([_eventType]);

    [Fact] void should_route_to_every_queue() => _queues.Count.ShouldEqual(QueueCount);

    [Fact] void should_include_all_queue_indices()
    {
        for (var queueIndex = 0; queueIndex < QueueCount; queueIndex++)
        {
            _queues.ShouldContain(queueIndex);
        }
    }
}
