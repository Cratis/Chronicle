// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_reloads_repeat_while_quarantine_entry_awaits : given.an_observer_entering_quarantine
{
    readonly given.reloadable_observer_state_storage _reloadable = new();
    readonly DeliveryClock _clock = new();
    readonly DateTimeOffset _entryTime = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    readonly List<DateTimeOffset?> _persistedTimestamps = [];

    public and_reloads_repeat_while_quarantine_entry_awaits()
    {
        _clock.Advance(_entryTime - DateTimeOffset.UnixEpoch);
        _timeProvider = _clock;
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _reloadable : null!;
    }

    async Task Because()
    {
        try
        {
            foreach (var timestamp in new DateTimeOffset?[] { _entryTime.AddDays(-1), null })
            {
                _reloadable.PersistedState = _stateStorage.State with { RunningState = Concepts.Observation.ObserverRunningState.Active };
                _persistedRunningStateChangedAt = timestamp;
                _clock.Advance(TimeSpan.FromSeconds(1));
                await _observer.Subscribe<NullObserverSubscriber>(Concepts.Observation.ObserverType.External, [Concepts.Events.EventType.Unknown], SiloAddress.Zero, automatic: true);
                await _observer.SetHandledStats(42UL);
                _persistedTimestamps.Add(_persistedRunningStateChangedAt);
            }
        }
        finally
        {
            _cleanupJobs.TrySetResult(_jobs);
        }
        await _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_persist_the_entry_time_during_each_reload() => _persistedTimestamps.ShouldEachConformTo(timestamp => timestamp == _entryTime);
    [Fact] void should_persist_quarantine_after_the_awaited_transition_finishes() => _reloadable.PersistedState.RunningState.ShouldEqual(Concepts.Observation.ObserverRunningState.Quarantined);
    [Fact] void should_not_restore_the_transition_hooks_stale_timestamp() => _persistedRunningStateChangedAt.ShouldEqual(_entryTime);
}
