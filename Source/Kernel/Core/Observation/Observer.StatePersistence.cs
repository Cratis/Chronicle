// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    readonly ObserverContinuationLifetime _continuationLifetime = new();

    /// <inheritdoc/>
    protected override async Task WriteStateAsync()
    {
        var checkpoint = _continuationLifetime.Capture();
        if (!checkpoint.IsCurrent || CannotWriteState()) return;
        await _stateWriteLock.WaitAsync();
        try
        {
            // Acquisition must establish release ownership before checking the captured lifetime.
            checkpoint.ThrowIfInvalidated();
            if (!CanContinueStateWrite()) return;
            PrepareStateWrite();
            var revision = _alertRevision;
            if (!await PersistStateWithOwnershipRecovery(checkpoint)) return;
            if (_runningStateChangedAtPending) await PersistRunningStateChangedAt(checkpoint);
            checkpoint.ThrowIfInvalidated();
            if (!CanContinueStateWrite()) return;
            RecordPersistedState(revision);
            ShareBindingWithSupervisor();
        }
        catch (ObserverContinuationInvalidated)
        {
            // Disposal discards this direct continuation, not the operation we have already joined.
        }
        finally
        {
            _stateWriteLock.Release();
        }
    }

    bool CanContinueStateWrite() => !_removed && !CannotWriteState();

    void PrepareStateWrite()
    {
        // The activation's quarantine outranks a reloaded snapshot's running state.
        if (IsQuarantined)
        {
            State = State with { RunningState = ObserverRunningState.Quarantined };
            _runningStateChangedAt = _quarantineRunningStateChangedAt;
        }

        // OnEnter may return an earlier record after a job callback advanced source-owned metadata.
        PreservePartitionRepairHolds();
        var shadowClaims = ShadowClaimsSnapshot();
        State = State with
        {
            AlertLifecycleId = _alertLifecycleId,
            AlertRevision = _alertRevision,
            AlertDisposition = _alertDisposition,
            QuarantineEpisodeId = _quarantineEpisodeId,
            WebhookProbe = _webhookProbe,
            DeliveryFences = DeliveryFenceSnapshot(),
            DurableSubscription = DurableSubscriptionToWrite(),
            PartitionRepairs = _partitionRepairs.Count == 0 ? null : _partitionRepairs.Values.ToArray(),
            PartitionClaims = shadowClaims.Claims,
            ObserverWideClaim = shadowClaims.Wide,
            OwnershipRevision = shadowClaims.Revision
        };
    }

    async Task<bool> PersistStateWithOwnershipRecovery(ObserverContinuationCheckpoint checkpoint)
    {
        try
        {
            await checkpoint.Await(base.WriteStateAsync);
        }
        catch (ObserverKeyDoesNotMatchDefinition) when (checkpoint.IsCurrent)
        {
            if (await checkpoint.Await(RelinquishOwnershipIfMoved)) return false;
            if (!CanContinueStateWrite()) return false;
            await checkpoint.Await(() => ObserverStateStorage.ClaimOwnership(_observerId, _observerKey.EventSequenceId));
            if (!CanContinueStateWrite()) return false;
            return await RetryStateWrite(checkpoint);
        }
        return CanContinueStateWrite();
    }

    async Task<bool> RetryStateWrite(ObserverContinuationCheckpoint checkpoint)
    {
        try
        {
            await checkpoint.Await(base.WriteStateAsync);
        }
        catch (ObserverKeyDoesNotMatchDefinition) when (checkpoint.IsCurrent)
        {
            if (await checkpoint.Await(RelinquishOwnershipIfMoved)) return false;
            throw;
        }
        return CanContinueStateWrite();
    }

    void RecordPersistedState(long revision)
    {
        _persistedStateWrites++;
        _forgottenDurableSubscriptionIsPending = false;
        PublishRunningStateGauge();
        if (revision == _alertRevision) _alertStateNeedsPersistence = false;
        _debouncedProgressWrites = 0;
    }
}
