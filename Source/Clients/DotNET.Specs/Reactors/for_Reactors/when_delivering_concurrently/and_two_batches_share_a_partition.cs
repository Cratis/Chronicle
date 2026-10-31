// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently;

public class and_two_batches_share_a_partition : given.a_delegate_with_gated_batches
{
    async Task Because()
    {
        Deliver("order-1", 1, 101);
        Deliver("order-1", 2, 102);
        Deliver("order-2", 3, 103);
        Release("order-1", 2);
        Release("order-2", 3);
        await Started("order-1", 1);
        await Answered(103);
        Release("order-1", 1);
        await Answered(102);
    }

    [Fact] void should_handle_the_second_batch_after_the_first() => IndexOf("order-1:2:start").ShouldBeGreaterThan(IndexOf("order-1:1:end"));
    [Fact] void should_answer_the_partitions_batches_in_arrival_order() => _results.Where(_ => _.Partition == "order-1").Select(_ => _.DeliveryId).ShouldEqual([101UL, 102UL]);
}
