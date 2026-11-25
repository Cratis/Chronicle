// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_routing;

public class a_batch_without_a_table : given.a_router_and_a_table
{
    IReadOnlyList<int> _queues;

    void Because() => _queues = _router.GetQueuesToDeliverTo(null, EventSequenceId.Log, [_unobserved.Id]);

    [Fact] void should_deliver_to_every_queue() => _queues.ShouldContainOnly(0, 1, 2, 3);
}
