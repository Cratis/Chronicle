// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_scheduling;

public class and_two_batches_share_a_partition : given.a_scheduler
{
    async Task Because()
    {
        var first = Schedule("a", "a1");
        var second = Schedule("a", "a2");
        var other = Schedule("b", "b1");
        Release("a2");
        Release("b1");
        await Started("a1");
        await Started("b1");
        Release("a1");
        await Task.WhenAll(first, second, other).WaitAsync(_deadline);
    }

    [Fact] void should_run_the_second_batch_after_the_first_has_finished() => IndexOf("a2:start").ShouldBeGreaterThan(IndexOf("a1:end"));
    [Fact] void should_run_another_partition_while_the_first_batch_is_running() => IndexOf("b1:start").ShouldBeLessThan(IndexOf("a1:end"));
}
