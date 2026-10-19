// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_delivering_concurrently;

public class and_the_bound_is_one : given.a_reducer_with_gated_partitions
{
    protected override int MaxConcurrentPartitions => 1;

    async Task Because()
    {
        Deliver("order-1", 1, 101);
        Deliver("order-2", 2, 102);
        Release("order-2");
        await Started("order-1");
        Release("order-1");
        await Answered(102);
    }

    [Fact] void should_reduce_the_second_partition_after_the_first() => IndexOf("order-2:start").ShouldBeGreaterThan(IndexOf("order-1:end"));
    [Fact] void should_answer_in_arrival_order() => _results.Select(_ => _.DeliveryId).ShouldEqual([101UL, 102UL]);
}
