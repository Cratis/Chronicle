// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// Once the bound on consecutive stranded-catch-up recoveries is exceeded, the observer must stop looping and
/// quarantine instead - turning a silent, permanent, zero-progress spin into a visible, operator-actionable state.
/// This is the proof that the fix for the permanent "preparing catch-up with no catch-up job" wedge actually
/// terminates: the observer reaches a settled end state within a bounded number of watchdog ticks, not never.
/// </summary>
public class and_starting_a_catchup_job_keeps_failing_past_the_retry_limit : given.an_observer_behind_on_a_relevant_event
{
    bool _isPreparingCatchupAfterTicks;

    async Task Because()
    {
        await _observer.CatchUp();
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts + 1; i++)
        {
            await _observer.RunWatchdogAsync();
        }

        _isPreparingCatchupAfterTicks = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);

    [Fact] void should_no_longer_be_preparing_catch_up() => _isPreparingCatchupAfterTicks.ShouldBeFalse();

    [Fact] void should_have_stopped_retrying_once_quarantined() => _jobsManager
        .Received(_observersConfig.MaxCatchupRecoveryAttempts + 1) // the manual seed plus one retry per tick up to (not including) the tick that quarantines
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
