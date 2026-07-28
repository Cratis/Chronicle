// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_debouncing_progress_persistence;

/// <summary>
/// The watchdog is the only bound on how long debounced progress can sit unpersisted on an observer that goes idle
/// below the batch interval — the interval flush needs more batches and the deactivation flush needs a deactivation,
/// so without the watchdog flush the durability window would be unbounded. Leaving the event sequence without an
/// actual tail makes the single write attributable to the flush alone: the watchdog's own
/// <c>CheckNextSequenceNumber()</c> returns on the unavailable tail, before any write of its own.
/// </summary>
public class and_the_watchdog_runs_with_pending_progress : given.an_observer_debouncing_progress_persistence
{
    int _writesBeforeWatchdogRan;

    void Establish() => _eventSequence.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);

    async Task Because()
    {
        // Two progress-only batches leave an advance debounced (below the interval), and the observer then goes idle.
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(0UL)]);
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(1UL)]);
        _writesBeforeWatchdogRan = _storageStats.Writes;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_leave_the_progress_unpersisted_until_the_watchdog_runs() => _writesBeforeWatchdogRan.ShouldEqual(0);
    [Fact] void should_flush_the_pending_progress_when_the_watchdog_runs() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_persist_the_advanced_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
}
