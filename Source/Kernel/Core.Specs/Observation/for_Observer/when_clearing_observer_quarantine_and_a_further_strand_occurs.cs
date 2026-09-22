// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Clearing quarantine resets the strand counter along with the state. Without the reset, the very
/// next stranded catch-up preparation put the observer straight back into quarantine - the counter
/// was already past the bound, so the manual clear survived exactly one transient hiccup and was
/// useless under the same load that caused the quarantine. The bound itself remains: keep stranding
/// past the limit and the observer quarantines again.
/// </summary>
public class when_clearing_observer_quarantine_and_a_further_strand_occurs : an_observer_behind_on_a_relevant_event
{
    bool _isQuarantinedAfterOneStrand;
    bool _isQuarantinedAfterExceedingTheBoundAgain;

    async Task Establish()
    {
        await _observer.CatchUp();
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts + 1; i++)
        {
            await _observer.RunWatchdogAsync();
        }
    }

    async Task Because()
    {
        await _observer.ClearObserverQuarantine();

        await _observer.RunWatchdogAsync();
        _isQuarantinedAfterOneStrand = await _observer.IsObserverQuarantined();

        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts + 1; i++)
        {
            await _observer.RunWatchdogAsync();
        }

        _isQuarantinedAfterExceedingTheBoundAgain = await _observer.IsObserverQuarantined();
    }

    [Fact] void should_keep_retrying_after_a_single_strand() => _isQuarantinedAfterOneStrand.ShouldBeFalse();
    [Fact] void should_quarantine_again_once_the_bound_is_exceeded_again() => _isQuarantinedAfterExceedingTheBoundAgain.ShouldBeTrue();
}
