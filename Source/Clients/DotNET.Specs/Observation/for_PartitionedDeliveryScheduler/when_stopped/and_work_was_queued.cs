// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_stopped;

public class and_work_was_queued : given.a_scheduler
{
    async Task Because()
    {
        var running = Schedule("a", "a1");
        var queued = Schedule("a", "a2");
        Release("a2");
        await Started("a1");
        Stop();
        Release("a1");
        await Task.WhenAll(running, queued).WaitAsync(_deadline);
    }

    [Fact] void should_let_running_work_finish() => IndexOf("a1:end").ShouldBeGreaterThan(-1);
    [Fact] void should_skip_queued_work() => IndexOf("a2:start").ShouldEqual(-1);
}
