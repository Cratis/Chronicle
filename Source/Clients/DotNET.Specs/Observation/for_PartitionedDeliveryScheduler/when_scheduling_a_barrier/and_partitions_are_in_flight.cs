// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_scheduling_a_barrier;

public class and_partitions_are_in_flight : given.a_scheduler
{
    async Task Because()
    {
        var first = Schedule("a", "a1");
        var second = Schedule("b", "b1");
        var barrier = ScheduleBarrier("replay");
        var afterOnSamePartition = Schedule("a", "a2");
        var afterOnNewPartition = Schedule("c", "c1");
        Release("replay");
        Release("a2");
        Release("c1");
        await Started("a1");
        await Started("b1");
        Release("a1");
        Release("b1");
        await Task.WhenAll(first, second, barrier, afterOnSamePartition, afterOnNewPartition).WaitAsync(_deadline);
    }

    [Fact] void should_wait_for_the_first_partition() => IndexOf("replay:start").ShouldBeGreaterThan(IndexOf("a1:end"));
    [Fact] void should_wait_for_the_second_partition() => IndexOf("replay:start").ShouldBeGreaterThan(IndexOf("b1:end"));
    [Fact] void should_hold_back_later_work_on_a_known_partition() => IndexOf("a2:start").ShouldBeGreaterThan(IndexOf("replay:end"));
    [Fact] void should_hold_back_later_work_on_a_new_partition() => IndexOf("c1:start").ShouldBeGreaterThan(IndexOf("replay:end"));
}
