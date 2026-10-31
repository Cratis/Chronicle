// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_draining;

public class and_a_batch_is_running_after_stop : given.a_scheduler
{
    Task _drain;
    bool _completedWhileRunning;

    async Task Because()
    {
        var running = Schedule("a", "a1");
        await Started("a1");
        Stop();
        _drain = Drain();
        _completedWhileRunning = _drain.IsCompleted;
        Release("a1");
        await Task.WhenAll(running, _drain).WaitAsync(_deadline);
    }

    [Fact] void should_not_complete_while_the_batch_is_running() => _completedWhileRunning.ShouldBeFalse();
    [Fact] void should_complete_once_the_batch_finished() => IndexOf("a1:end").ShouldBeGreaterThan(-1);
}
