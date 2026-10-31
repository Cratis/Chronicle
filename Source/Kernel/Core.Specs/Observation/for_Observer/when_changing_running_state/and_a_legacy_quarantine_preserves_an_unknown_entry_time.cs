// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_legacy_quarantine_preserves_an_unknown_entry_time : given.an_observer
{
    DateTimeOffset? _afterActivation;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Quarantined, RunningStateChangedAt = null };
        await Crash();
        _afterActivation = _stateStorage.State.RunningStateChangedAt;
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Active, RunningStateChangedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero) };
    }

    Task Because() => _observer.SetHandledStats(42UL);

    [Fact] void should_not_invent_an_entry_time_on_activation() => _afterActivation.ShouldBeNull();
    [Fact] void should_restore_quarantine_on_write() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_entry_time_unknown() => _stateStorage.State.RunningStateChangedAt.ShouldBeNull();
}
