// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_routing;

public class a_batch_of_an_observed_event_type : given.a_router_and_a_table
{
    IReadOnlyList<int> _queues;

    void Because() => _queues = _router.GetQueuesToDeliverTo(_table, EventSequenceId.Log, [_typed.Id]);

    [Fact] void should_deliver_to_the_queues_of_its_observers_and_of_observers_of_every_event_type() => _queues.ShouldContainOnly(1, 3);
}
