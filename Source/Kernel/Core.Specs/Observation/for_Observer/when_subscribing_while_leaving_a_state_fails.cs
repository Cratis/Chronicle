// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_subscribing_while_leaving_a_state_fails : given.an_observer_with_a_failing_leave
{
    async Task Because()
    {
        var recoveryTimerIndex = _silo.TimerRegistry.Mock.Invocations.Count;
        await FailLeave(subscribeWhileLeaving: true);
        await _jobsManager.DidNotReceive().GetUnfinishedJobs();
        await _silo.TimerRegistry.FireAsync(recoveryTimerIndex);
        await _jobsManager.Received(1).GetUnfinishedJobs();
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_preserve_the_leave_failure() => _transitionFailure.ShouldEqual(_failure);
    [Fact] async Task should_promptly_recover_the_subscription_once() => await _jobsManager.Received(1).GetUnfinishedJobs();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_disarm_the_recovery_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
