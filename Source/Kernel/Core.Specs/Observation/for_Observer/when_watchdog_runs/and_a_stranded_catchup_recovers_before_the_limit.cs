// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// The consecutive-failure count that bounds stranded-catch-up recovery must track genuine, ongoing failure - not
/// accumulate forever across unrelated incidents. A catch-up job that eventually does prepare its steps is real
/// forward progress, and must reset the count, so a later, distinct run of failures gets its own full budget of
/// retries rather than inheriting an already-exhausted one.
/// </summary>
public class and_a_stranded_catchup_recovers_before_the_limit : given.an_observer_behind_on_a_relevant_event
{
    async Task Because()
    {
        // First incident: fails right up to (not past) the retry limit.
        await _observer.CatchUp();
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts; i++)
        {
            await _observer.RunWatchdogAsync();
        }

        // Genuine forward progress - a job actually prepared its steps - resets the consecutive-failure count.
        // An empty partition set is enough to reach that reset without pulling in the separate
        // "catching-up partitions with no running job" watchdog path.
        await _observer.RegisterCatchingUpPartitions([]);

        // Second, distinct incident: also fails right up to the retry limit. Without the reset above this would
        // exceed the limit on the very first of these ticks, since it follows immediately after the first incident's
        // count already sat at the limit.
        await _observer.CatchUp();
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts; i++)
        {
            await _observer.RunWatchdogAsync();
        }
    }

    [Fact] void should_not_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);

    [Fact] void should_leave_the_observer_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
