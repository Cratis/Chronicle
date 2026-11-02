// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_persisted_quarantine_preserves_its_entry_time : given.an_observer
{
    readonly DateTimeOffset _quarantineTime = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Quarantined };
        _persistedRunningStateChangedAt = _quarantineTime;
        _eventStoreNamespaceStorage.Observers.ClearReceivedCalls();
        await Crash();
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Active };
    }

    Task Because() => _observer.SetHandledStats(42UL);

    [Fact] void should_not_restamp_on_activation() => _eventStoreNamespaceStorage.Observers.DidNotReceive().SaveRunningStateChangedAt(Arg.Any<ObserverId>(), Arg.Any<DateTimeOffset?>(), Arg.Any<EventSequenceId>());
    [Fact] void should_restore_quarantine_on_write() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_preserve_the_persisted_entry_time() => _persistedRunningStateChangedAt.ShouldEqual(_quarantineTime);
}
