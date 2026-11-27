// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_fresh_subscription_recovers_after_leaving_failed : given.an_observer_with_a_failing_leave
{
    async Task Because()
    {
        await FailLeave(subscribeWhileLeaving: true);
        await Subscribe().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_not_repeat_recovery_when_the_owed_drain_runs() => await _jobsManager.Received(1).GetUnfinishedJobs();
    [Fact] void should_disarm_the_recovery_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
