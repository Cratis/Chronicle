// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_routing;

public class a_batch_no_observer_takes : given.a_router_and_a_table
{
    IReadOnlyList<int> _queues;

    void Because() => _queues = _router.GetQueuesToDeliverTo(_table, EventSequenceId.Outbox, [_typed.Id]);

    [Fact] void should_deliver_to_no_queue() => _queues.ShouldBeEmpty();
}
