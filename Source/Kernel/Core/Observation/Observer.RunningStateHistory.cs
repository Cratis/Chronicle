// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The last settled running state, seeded on activation.
    /// </summary>
    ObserverRunningState _settledRunningState;

    /// <summary>
    /// When the running state last changed. Kept outside <see cref="ObserverState"/> so the per-batch state copies do not carry it.
    /// </summary>
    DateTimeOffset? _runningStateChangedAt;

    /// <summary>
    /// The activation owns this entry time, independently of reloaded progress snapshots.
    /// </summary>
    DateTimeOffset? _quarantineRunningStateChangedAt;

    /// <summary>
    /// Whether <see cref="_runningStateChangedAt"/> still has to be written to storage.
    /// </summary>
    bool _runningStateChangedAtPending;

    IObserverStateStorage ObserverStateStorage =>
        storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Observers;

    async Task SeedRunningStateHistory()
    {
        _settledRunningState = State.RunningState;
        _runningStateChangedAt = await ObserverStateStorage.GetRunningStateChangedAt(_observerId);
        _quarantineRunningStateChangedAt = _settledRunningState == ObserverRunningState.Quarantined ? _runningStateChangedAt : null;
    }

    void ApplyRunningState(ObserverRunningState next)
    {
        var wasQuarantined = _settledRunningState == ObserverRunningState.Quarantined;
        var (changedAt, settled, changed) = ObserverRunningStateChanges.Apply(_runningStateChangedAt, _settledRunningState, next, _clock.GetUtcNow());
        _runningStateChangedAt = changedAt;
        _settledRunningState = settled;
        if (changed) _runningStateChangedAtPending = true;

        if (!_isQuarantined) return;
        if (!wasQuarantined) _quarantineRunningStateChangedAt = _runningStateChangedAt;
        _runningStateChangedAt = _quarantineRunningStateChangedAt;
    }

    async Task ReloadRunningStateHistory(bool storageWasReset)
    {
        if (storageWasReset)
        {
            _runningStateChangedAt = null;
            _runningStateChangedAtPending = false;
            return;
        }

        if (IsQuarantined)
        {
            _runningStateChangedAt = _quarantineRunningStateChangedAt;
            _runningStateChangedAtPending = true;
            return;
        }

        if (!_runningStateChangedAtPending)
        {
            _runningStateChangedAt = await ObserverStateStorage.GetRunningStateChangedAt(_observerId);
        }
    }

    async Task PersistRunningStateChangedAt()
    {
        // A refusal means another owner holds the record; do not retry, that would become a per-batch loop.
        _runningStateChangedAtPending = false;
        try
        {
            await ObserverStateStorage.SaveRunningStateChangedAt(_observerId, _runningStateChangedAt, _observerKey.EventSequenceId);
        }
        catch
        {
            _runningStateChangedAtPending = true;
            throw;
        }
    }
}
