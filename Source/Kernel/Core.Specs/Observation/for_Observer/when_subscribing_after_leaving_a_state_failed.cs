// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_subscribing_after_leaving_a_state_failed : given.an_observer_with_a_failing_leave
{
    int _activeTimersBefore;
    int _timerRegistrationsBefore;

    void Establish()
    {
        _activeTimersBefore = _silo.TimerRegistry.NumberOfActiveTimers;
        _timerRegistrationsBefore = _silo.TimerRegistry.Mock.Invocations.Count;
    }

    async Task Because()
    {
        await FailLeave(subscribeWhileLeaving: false);
        await Subscribe().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_preserve_the_leave_failure() => _transitionFailure.ShouldEqual(_failure);
    [Fact] async Task should_recover_without_waiting_for_another_state_entry() => await _jobsManager.Received(1).GetUnfinishedJobs();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_not_defer_recovery_to_a_timer() => _silo.TimerRegistry.Mock.Invocations.Count.ShouldEqual(_timerRegistrationsBefore);
    [Fact] void should_preserve_existing_timers() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(_activeTimersBefore);
}
