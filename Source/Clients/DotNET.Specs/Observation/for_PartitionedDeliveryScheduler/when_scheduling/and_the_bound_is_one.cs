// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_scheduling;

public class and_the_bound_is_one : given.a_scheduler
{
    protected override int MaxConcurrentPartitions => 1;

    async Task Because()
    {
        var first = Schedule("a", "a1");
        var second = Schedule("b", "b1");
        var third = Schedule("c", "c1");
        Release("b1");
        Release("c1");
        await Started("a1");
        Release("a1");
        await Task.WhenAll(first, second, third).WaitAsync(_deadline);
    }

    [Fact] void should_run_one_batch_at_a_time() => _maxRunning.ShouldEqual(1);
    [Fact] void should_run_batches_in_arrival_order() => _log.ToArray().ShouldEqual(["a1:start", "a1:end", "b1:start", "b1:end", "c1:start", "c1:end"]);
}
