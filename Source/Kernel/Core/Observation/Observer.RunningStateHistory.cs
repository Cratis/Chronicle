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

    /// <summary>Orders this activation's history updates across interleaved reads, writes and transitions.</summary>
    ulong _runningStateHistoryRevision;

    IObserverStateStorage ObserverStateStorage =>
        storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Observers;

    async Task<bool> SeedRunningStateHistory()
    {
        var checkpoint = _continuationLifetime.Capture();
        if (!checkpoint.IsCurrent || _removed) return false;
        _settledRunningState = State.RunningState;
        var revision = ++_runningStateHistoryRevision;
        try
        {
            var changedAt = await checkpoint.Await(() => ObserverStateStorage.GetRunningStateChangedAt(_observerId));
            if (!CanApplyRunningStateHistory(revision)) return true;
            _runningStateChangedAt = changedAt;
            _quarantineRunningStateChangedAt = _settledRunningState == ObserverRunningState.Quarantined ? _runningStateChangedAt : null;
            return true;
        }
        catch (ObserverContinuationInvalidated)
        {
            // The activation caller must not resume initialization after a discarded seed.
            return false;
        }
    }

    void ApplyRunningState(ObserverRunningState next)
    {
        _runningStateHistoryRevision++;
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
        var checkpoint = _continuationLifetime.Capture();
        if (!checkpoint.IsCurrent || _removed) return;
        var revision = ++_runningStateHistoryRevision;
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
            try
            {
                var changedAt = await checkpoint.Await(() => ObserverStateStorage.GetRunningStateChangedAt(_observerId));
                if (CanApplyRunningStateHistory(revision)) _runningStateChangedAt = changedAt;
            }
            catch (ObserverContinuationInvalidated)
            {
                // Only this direct history continuation is discarded; provider-internal effects are not fenced.
            }
        }
    }

    bool CanApplyRunningStateHistory(ulong revision) =>
        !_removed && revision == _runningStateHistoryRevision;

    async Task PersistRunningStateChangedAt(ObserverContinuationCheckpoint checkpoint)
    {
        checkpoint.ThrowIfInvalidated();
        if (!CanContinueStateWrite()) return;
        var revision = _runningStateHistoryRevision;
        await checkpoint.Await(() => ObserverStateStorage.SaveRunningStateChangedAt(_observerId, _runningStateChangedAt, _observerKey.EventSequenceId));

        // A refusal is settled too: retrying it would become a per-batch loop. A failure leaves the debt intact,
        // while a newer transition or reload owns its own debt, irrespective of this write's outcome.
        if (CanContinueStateWrite() && CanApplyRunningStateHistory(revision)) _runningStateChangedAtPending = false;
    }
}
