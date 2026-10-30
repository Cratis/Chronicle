// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Claims;

/// <summary>
/// Tracks diagnostic ownership claims while replay and catch-up sets remain authoritative.
/// </summary>
internal sealed class ShadowClaimLedger
{
    readonly Dictionary<Key, PartitionClaim> _claims = [];
    ObserverWideClaim? _wide;
    OwnershipRevision _revision = OwnershipRevision.None;

    /// <summary>
    /// Restores stored claims, identifying records predating the shadow protocol.
    /// </summary>
    /// <param name="claims">The stored partition claims, or null for legacy state.</param>
    /// <param name="wide">The stored observer-wide claim.</param>
    /// <param name="revision">The stored ownership revision.</param>
    /// <param name="legacy">Whether all shadow fields were absent.</param>
    /// <returns>The restored ledger.</returns>
    internal static ShadowClaimLedger Restore(IEnumerable<PartitionClaim>? claims, ObserverWideClaim? wide, OwnershipRevision? revision, out bool legacy)
    {
        legacy = claims is null && wide is null && revision is null;
        var ledger = new ShadowClaimLedger { _wide = wide, _revision = revision ?? OwnershipRevision.None };
        foreach (var claim in claims ?? [])
        {
            ledger._claims[claim.Partition] = claim;
            ledger._revision = new(Math.Max(ledger._revision.Value, claim.Epoch.Value));
        }

        ledger._revision = new(Math.Max(ledger._revision.Value, wide?.Epoch.Value ?? 0));
        return ledger;
    }

    /// <summary>
    /// Compares claims with authoritative sets without changing the ledger.
    /// </summary>
    /// <param name="replaying">The replaying partitions.</param>
    /// <param name="catchingUp">The catching-up partitions.</param>
    /// <param name="observerReplaying">Whether the observer is replaying.</param>
    /// <returns>The diagnostic disagreements.</returns>
    internal IReadOnlyList<(Key? Partition, ShadowClaimDivergence Divergence)> Compare(ISet<Key> replaying, ISet<Key> catchingUp, bool observerReplaying)
    {
        List<(Key? Partition, ShadowClaimDivergence Divergence)> divergences = [];
        foreach (var claim in _claims.Values.Where(_ => _.Holder is ClaimHolderKind.PartitionReplay or ClaimHolderKind.PartitionCatchUp))
        {
            var matching = claim.Holder == ClaimHolderKind.PartitionReplay ? replaying : catchingUp;
            var opposite = claim.Holder == ClaimHolderKind.PartitionReplay ? catchingUp : replaying;
            if (!matching.Contains(claim.Partition))
            {
                divergences.Add((claim.Partition, opposite.Contains(claim.Partition) ? ShadowClaimDivergence.KindMismatch : ShadowClaimDivergence.ClaimWithoutDerivedSet));
            }
        }

        AddMissingClaims(replaying, ClaimHolderKind.ObserverReplay, divergences);
        AddMissingClaims(catchingUp, ClaimHolderKind.ObserverCatchUp, divergences);
        if (observerReplaying != (_wide?.Holder == ClaimHolderKind.ObserverReplay))
        {
            divergences.Add((null, observerReplaying ? ShadowClaimDivergence.DerivedSetWithoutClaim : ShadowClaimDivergence.ClaimWithoutDerivedSet));
        }

        return divergences;
    }

    /// <summary>
    /// Issues a fresh epoch even when the same partition is granted again.
    /// </summary>
    /// <param name="partition">The claimed partition.</param>
    /// <param name="kind">The work holding the claim.</param>
    /// <param name="job">The job, when known.</param>
    /// <param name="resume">The resume position.</param>
    /// <returns>Any exclusion violation, without preventing the grant.</returns>
    internal IReadOnlyList<(Key? Partition, ShadowClaimDivergence Divergence)> Grant(Key partition, ClaimHolderKind kind, JobId? job, EventSequenceNumber resume)
    {
        _claims[partition] = new(partition, kind, job, NextEpoch(), resume, []);
        return _wide is null ? [] : [(partition, ShadowClaimDivergence.ExclusionViolated)];
    }

    /// <summary>
    /// Issues an observer-wide claim without enforcing exclusion.
    /// </summary>
    /// <param name="kind">The work holding the claim.</param>
    /// <param name="job">The job, when known.</param>
    /// <param name="resume">The resume position.</param>
    /// <returns>An exclusion violation for every held partition claim.</returns>
    internal IReadOnlyList<(Key? Partition, ShadowClaimDivergence Divergence)> GrantObserverWide(ClaimHolderKind kind, JobId? job, EventSequenceNumber resume)
    {
        _wide = new(kind, job, NextEpoch(), resume, []);
        return _claims.Keys.Select(_ => ((Key?)_, ShadowClaimDivergence.ExclusionViolated)).ToArray();
    }

    /// <summary>
    /// Releases a partition claim, advancing the revision only when it was held.
    /// </summary>
    /// <param name="partition">The partition handed over.</param>
    internal void HandOver(Key partition)
    {
        if (_claims.Remove(partition))
        {
            _revision = _revision.Next();
        }
    }

    /// <summary>
    /// Releases the observer-wide claim, advancing the revision only when it was held.
    /// </summary>
    internal void HandOverObserverWide()
    {
        if (_wide is not null)
        {
            _wide = null;
            _revision = _revision.Next();
        }
    }

    /// <summary>
    /// Aligns shadow claims with derived sets, reporting implicit partition releases only.
    /// </summary>
    /// <param name="replaying">The authoritative replaying partitions.</param>
    /// <param name="catchingUp">The authoritative catching-up partitions.</param>
    /// <param name="observerReplaying">Whether the observer is replaying.</param>
    /// <param name="defaultResume">The resume position for derived grants.</param>
    /// <returns>The claims released without an explicit hand-over.</returns>
    internal IReadOnlyList<(Key? Partition, ShadowClaimDivergence Divergence)> Reconcile(ISet<Key> replaying, ISet<Key> catchingUp, bool observerReplaying, EventSequenceNumber defaultResume)
    {
        var divergences = ReconcileHeldClaims(replaying, catchingUp, defaultResume);
        foreach (var partition in replaying.Where(_ => !_claims.ContainsKey(_)))
        {
            Grant(partition, ClaimHolderKind.PartitionReplay, null, defaultResume);
        }

        if (_wide?.Holder != ClaimHolderKind.ObserverCatchUp)
        {
            foreach (var partition in catchingUp.Where(_ => !_claims.ContainsKey(_)))
            {
                Grant(partition, ClaimHolderKind.PartitionCatchUp, null, defaultResume);
            }
        }

        if (observerReplaying && _wide?.Holder != ClaimHolderKind.ObserverReplay)
        {
            GrantObserverWide(ClaimHolderKind.ObserverReplay, null, defaultResume);
        }
        else if (!observerReplaying && _wide?.Holder == ClaimHolderKind.ObserverReplay)
        {
            HandOverObserverWide();
        }

        return divergences;
    }

    /// <summary>
    /// Captures claims ordered by partition string for deterministic persistence.
    /// </summary>
    /// <returns>The partition claims, observer-wide claim, and current revision.</returns>
    internal (IReadOnlyList<PartitionClaim> Claims, ObserverWideClaim? Wide, OwnershipRevision Revision) Snapshot() =>
        (_claims.Values.OrderBy(_ => _.Partition.ToString(), StringComparer.Ordinal).ToArray(), _wide, _revision);

    static ClaimHolderKind KindFromSets(Key partition, ISet<Key> replaying, ISet<Key> catchingUp)
    {
        if (replaying.Contains(partition))
        {
            return ClaimHolderKind.PartitionReplay;
        }

        return catchingUp.Contains(partition) ? ClaimHolderKind.PartitionCatchUp : ClaimHolderKind.None;
    }

    void AddMissingClaims(ISet<Key> partitions, ClaimHolderKind coveringKind, List<(Key? Partition, ShadowClaimDivergence Divergence)> divergences)
    {
        if (_wide?.Holder == coveringKind)
        {
            return;
        }

        divergences.AddRange(partitions.Where(_ => !_claims.ContainsKey(_)).Select(_ => ((Key?)_, ShadowClaimDivergence.DerivedSetWithoutClaim)));
    }

    List<(Key? Partition, ShadowClaimDivergence Divergence)> ReconcileHeldClaims(ISet<Key> replaying, ISet<Key> catchingUp, EventSequenceNumber resume)
    {
        List<(Key? Partition, ShadowClaimDivergence Divergence)> divergences = [];
        foreach (var claim in _claims.Values.Where(_ => _.Holder is ClaimHolderKind.PartitionReplay or ClaimHolderKind.PartitionCatchUp).ToArray())
        {
            var kind = KindFromSets(claim.Partition, replaying, catchingUp);
            if (kind == ClaimHolderKind.None)
            {
                HandOver(claim.Partition);
                divergences.Add((claim.Partition, ShadowClaimDivergence.ReleasedWithoutHandOver));
            }
            else if (kind != claim.Holder)
            {
                Grant(claim.Partition, kind, null, resume);
            }
        }

        return divergences;
    }

    ClaimEpoch NextEpoch()
    {
        _revision = _revision.Next();
        return new(_revision.Value);
    }
}
