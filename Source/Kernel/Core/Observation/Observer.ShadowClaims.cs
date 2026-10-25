// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Claims;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

#pragma warning disable RCS1075 // Shadow diagnostics are write-only: a failing logger must never affect the observer

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    ShadowClaimLedger _shadowClaims = new();

    void RestoreShadowClaims() => GuardShadowClaims(() =>
    {
        _shadowClaims = ShadowClaimLedger.Restore(State.PartitionClaims, State.ObserverWideClaim, State.OwnershipRevision, out var legacy);
        if (legacy)
        {
            logger.ShadowClaimsSeededFromLegacyState(_observerId);
        }
        else
        {
            ReportShadowDivergences(_shadowClaims.Compare(State.ReplayingPartitions, State.CatchingUpPartitions, IsObserverReplaying()));
        }

        // Compare already reported stale claims. Reconciliation on activation must not count them a second time.
        _shadowClaims.Reconcile(State.ReplayingPartitions, State.CatchingUpPartitions, IsObserverReplaying(), ShadowResumePosition());
    });

    (IEnumerable<PartitionClaim>? Claims, ObserverWideClaim? Wide, OwnershipRevision? Revision) ShadowClaimsSnapshot()
    {
        var snapshot = (Claims: State.PartitionClaims, Wide: State.ObserverWideClaim, Revision: State.OwnershipRevision);
        GuardShadowClaims(() =>
        {
            // Reconcile reports stale releases, but silently seeds missing claims. Report those before they are repaired.
            ReportShadowDivergences(_shadowClaims.Compare(State.ReplayingPartitions, State.CatchingUpPartitions, IsObserverReplaying())
                .Where(_ => _.Divergence == ShadowClaimDivergence.DerivedSetWithoutClaim));
            ReportShadowDivergences(_shadowClaims.Reconcile(State.ReplayingPartitions, State.CatchingUpPartitions, IsObserverReplaying(), ShadowResumePosition()));
            snapshot = _shadowClaims.Snapshot();
        });
        return snapshot;
    }

    bool IsObserverReplaying() => State.IsReplaying || State.RunningState == ObserverRunningState.Replaying;

    EventSequenceNumber ShadowResumePosition() => State.LastHandledEventSequenceNumber.IsActualValue
        ? State.LastHandledEventSequenceNumber.Next()
        : EventSequenceNumber.First;

    void ShadowGrantPartition(Key partition, ClaimHolderKind kind, JobId? job, EventSequenceNumber resume) =>
        GuardShadowClaims(() => ReportShadowDivergences(_shadowClaims.Grant(partition, kind, job, resume)));

    void ShadowHandOverPartition(Key partition) => GuardShadowClaims(() => _shadowClaims.HandOver(partition));

    void ShadowGrantObserverWide(ClaimHolderKind kind, JobId? job, EventSequenceNumber resume) =>
        GuardShadowClaims(() => ReportShadowDivergences(_shadowClaims.GrantObserverWide(kind, job, resume)));

    void ShadowHandOverObserverWide() => GuardShadowClaims(_shadowClaims.HandOverObserverWide);

    void ReportShadowDivergences(IEnumerable<(Key? Partition, ShadowClaimDivergence Divergence)> divergences)
    {
        foreach (var (partition, divergence) in divergences)
        {
            logger.ShadowClaimDivergence(_observerId, divergence, partition?.ToString());
            _metrics?.ClaimDivergence(divergence);
        }
    }

    /// <summary>
    /// Isolates shadow diagnostics from delivery, including failures in diagnostic logging itself.
    /// </summary>
    /// <param name="action">The shadow-only operation.</param>
    /// <remarks>
    /// Shadow claims have no effect boundary: they are diagnostics only, never authority for delivery.
    /// This deliberate exception to effect-boundary error handling must not change observer behavior.
    /// </remarks>
    void GuardShadowClaims(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            try
            {
                logger.ShadowClaimsFailed(_observerId, exception);
            }
            catch (Exception)
            {
                // A failing diagnostic logger must not turn write-only shadow data into a delivery failure.
            }
        }
    }
}
