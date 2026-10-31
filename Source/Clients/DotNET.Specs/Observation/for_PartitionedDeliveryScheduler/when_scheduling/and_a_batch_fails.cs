// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_scheduling;

public class and_a_batch_fails : given.a_scheduler
{
    Exception _error;

    async Task Because()
    {
        var failing = Schedule("a", "a1");
        var queued = Schedule("a", "a2");
        Release("a2");
        await Started("a1");
        Fail("a1");
        _error = await Catch.Exception(() => failing.WaitAsync(_deadline));
        await queued.WaitAsync(_deadline);
        Release("b1");
        await Schedule("b", "b1").WaitAsync(_deadline);
    }

    [Fact] void should_fail_with_the_batch_error() => _error.ShouldBeOfExactType<WorkFailed>();
    [Fact] void should_skip_the_batch_queued_behind_it() => IndexOf("a2:start").ShouldEqual(-1);
    [Fact] void should_skip_work_scheduled_after_the_failure() => IndexOf("b1:start").ShouldEqual(-1);
}
