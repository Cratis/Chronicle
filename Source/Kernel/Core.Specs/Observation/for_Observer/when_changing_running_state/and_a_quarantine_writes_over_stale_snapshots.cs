// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_quarantine_writes_over_stale_snapshots : given.an_observer_with_a_quarantine_clock
{
    readonly List<DateTimeOffset?> _timestamps = [];
    readonly List<ObserverRunningState> _runningStates = [];

    async Task Establish()
    {
        _quarantineClock.Advance(TimeSpan.FromHours(1));
        await _observer.TransitionTo<QuarantinedObserver>();
    }

    async Task Because()
    {
        foreach (var timestamp in new DateTimeOffset?[] { _quarantineTime.AddDays(-1), null, _quarantineTime.AddDays(-2), null })
        {
            _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Active, RunningStateChangedAt = timestamp };
            _quarantineClock.Advance(TimeSpan.FromSeconds(1));
            await _observer.SetHandledStats(42UL);
            _timestamps.Add(_reloadableStateStorage.PersistedState.RunningStateChangedAt);
            _runningStates.Add(_reloadableStateStorage.PersistedState.RunningState);
        }
    }

    [Fact] void should_preserve_the_entry_time_on_every_write() => _timestamps.ShouldEachConformTo(timestamp => timestamp == _quarantineTime);
    [Fact] void should_preserve_quarantine_on_every_write() => _runningStates.ShouldEachConformTo(state => state == ObserverRunningState.Quarantined);
    [Fact] void should_still_record_progress() => _reloadableStateStorage.PersistedState.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
