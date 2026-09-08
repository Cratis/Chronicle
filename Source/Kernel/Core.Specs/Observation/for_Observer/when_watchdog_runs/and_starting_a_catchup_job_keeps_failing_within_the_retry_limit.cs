// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// Starting a catch-up job can fail for a structural reason no retry will fix, which used to leave the observer
/// looping forever between the stranded-preparation rescue and a fresh, doomed catch-up attempt - one recovery, one
/// re-route, one failed <c>CatchUp</c>, repeated with zero forward progress. Up to the configured bound, though, the
/// loop must keep retrying rather than giving up early: a bound reached prematurely would quarantine an observer
/// that was only ever going to need one more attempt.
/// </summary>
public class and_starting_a_catchup_job_keeps_failing_within_the_retry_limit : given.an_observer_behind_on_a_relevant_event
{
    bool _isPreparingCatchupAfterTicks;

    async Task Because()
    {
        // Every Start<ICatchUpObserver,...> call is unconfigured, so every attempt to start a catch-up job fails -
        // simulating a persistent, structural failure rather than a one-off transient one.
        await _observer.CatchUp();
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts; i++)
        {
            await _observer.RunWatchdogAsync();
        }

        _isPreparingCatchupAfterTicks = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_not_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);

    [Fact] void should_leave_the_observer_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact] void should_still_be_preparing_catch_up() => _isPreparingCatchupAfterTicks.ShouldBeTrue();

    [Fact] void should_have_kept_retrying_the_catch_up() => _jobsManager
        .Received(_observersConfig.MaxCatchupRecoveryAttempts + 1) // the manual seed plus one retry per watchdog tick
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
