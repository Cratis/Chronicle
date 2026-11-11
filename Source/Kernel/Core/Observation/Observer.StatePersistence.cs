// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool CannotWriteState() => _stateWritingSuspended || !_observerExists || !KeyOwnsDefinition;

    void PrepareObserverStateForWrite()
    {
        // A quarantine owned by this activation outranks whatever running state a reloaded snapshot carried,
        // so any write while quarantined persists the quarantine rather than a stale active snapshot.
        if (IsQuarantined)
        {
            State = State with { RunningState = ObserverRunningState.Quarantined };
            _runningStateChangedAt = _quarantineRunningStateChangedAt;
        }

        // A state-machine OnEnter can return an earlier record after awaiting a job callback. Preserve the
        // source-owned metadata advanced by that callback, and serialize writes from AlwaysInterleave methods.
        var shadowClaims = ShadowClaimsSnapshot();
        State = State with
        {
            AlertLifecycleId = _alertLifecycleId,
            AlertRevision = _alertRevision,
            AlertDisposition = _alertDisposition,
            QuarantineEpisodeId = _quarantineEpisodeId,
            WebhookProbe = _webhookProbe,
            DeliveryFences = DeliveryFenceSnapshot(),
            PartitionClaims = shadowClaims.Claims,
            ObserverWideClaim = shadowClaims.Wide,
            OwnershipRevision = shadowClaims.Revision
        };
    }

    async Task<bool> WriteWithOwnershipRetry()
    {
        try
        {
            await WriteObserverStateAndRunningTime();
        }
        catch (ObserverKeyDoesNotMatchDefinition)
        {
            if (await RelinquishOwnershipIfMoved()) return false;
            await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace)
                .Observers.ClaimOwnership(_observerId, _observerKey.EventSequenceId);
            try
            {
                await WriteObserverStateAndRunningTime();
            }
            catch (ObserverKeyDoesNotMatchDefinition)
            {
                if (await RelinquishOwnershipIfMoved()) return false;
                throw;
            }
        }
        return true;
    }

    async Task WriteObserverStateAndRunningTime()
    {
        await base.WriteStateAsync();
        if (_runningStateChangedAtPending) await PersistRunningStateChangedAt();
    }

    void CompleteObserverStateWrite(long revision)
    {
        _persistedStateWrites++;
        PublishRunningStateGauge();
        if (revision == _alertRevision)
        {
            _alertStateNeedsPersistence = false;
        }

        _debouncedProgressWrites = 0;
    }

    class WriteSuspension : IDisposable
    {
        readonly Observer _observer;

        public WriteSuspension(Observer observer)
        {
            _observer = observer;
            _observer._stateWritingSuspended = true;
        }

        public void Dispose() => _observer._stateWritingSuspended = false;
    }
}
