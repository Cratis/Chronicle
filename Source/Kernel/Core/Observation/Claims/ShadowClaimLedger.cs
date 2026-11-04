// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims;

/// <summary>
/// Tracks diagnostic ownership claims while replay and catch-up sets remain authoritative.
/// </summary>
internal sealed class ShadowClaimLedger
{
    readonly Dictionary<Key, PartitionClaim> _claims = [];
    ObserverWideClaim? _wide;
    OwnershipRevision _revision = OwnershipRevision.None;
    PartitionClaim[]? _orderedClaims;

    /// <summary>
    /// Gets the observer-wide claim, if any.
    /// </summary>
    internal ObserverWideClaim? ObserverWide => _wide;

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
            ledger.SetClaim(claim.Partition, claim);
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
        SetClaim(partition, new(partition, kind, job, NextEpoch(), resume, []));
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
        if (_claims.Count == 0)
        {
            return [];
        }

        var violations = new (Key? Partition, ShadowClaimDivergence Divergence)[_claims.Count];
        var index = 0;
        foreach (var partition in _claims.Keys)
        {
            violations[index++] = (partition, ShadowClaimDivergence.ExclusionViolated);
        }

        return violations;
    }

    /// <summary>
    /// Gets the claim held for a partition, if any.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <returns>The <see cref="PartitionClaim"/>, or null when the partition is not claimed.</returns>
    internal PartitionClaim? ClaimFor(Key partition) => _claims.GetValueOrDefault(partition);

    /// <summary>
    /// Records the job started under the observer-wide claim granted with the given epoch.
    /// </summary>
    /// <param name="epoch">The epoch the job was started under.</param>
    /// <param name="job">The <see cref="JobId"/> of the job.</param>
    /// <remarks>
    /// Changes neither the epoch nor the revision. A claim granted since then is left as it is.
    /// </remarks>
    internal void AttachObserverWideJob(ClaimEpoch epoch, JobId job)
    {
        if (_wide is not null && _wide.Epoch == epoch)
        {
            _wide = _wide with { Job = job };
        }
    }

    /// <summary>
    /// Releases a partition claim of the given kind, leaving a claim of any other kind in place.
    /// </summary>
    /// <param name="partition">The partition handed over.</param>
    /// <param name="holder">The <see cref="ClaimHolderKind"/> handing it over.</param>
    internal void HandOver(Key partition, ClaimHolderKind holder)
    {
        if (_claims.TryGetValue(partition, out var claim) && claim.Holder == holder)
        {
            HandOver(partition);
        }
    }

    /// <summary>
    /// Releases a partition claim, advancing the revision only when it was held.
    /// </summary>
    /// <param name="partition">The partition handed over.</param>
    internal void HandOver(Key partition)
    {
        if (RemoveClaim(partition))
        {
            _revision = _revision.Next();
        }
    }

    /// <summary>
    /// Gets whether the claim of the given kind on a scope belongs to other work than the job reporting back.
    /// </summary>
    /// <param name="holder">The <see cref="ClaimHolderKind"/> the reporting job runs as.</param>
    /// <param name="partition">The partition, or null for the observer-wide claim.</param>
    /// <param name="job">The reporting <see cref="JobId"/>, or null when it is not known.</param>
    /// <param name="epoch">The ownership epoch the reporting job was started under, or null when it carries none.</param>
    /// <returns>True when such a claim is held under another job or epoch; false when none is held or it cannot be told.</returns>
    /// <remarks>
    /// With no claim of that kind there is no newer work to protect, so a report is never judged stale for it.
    /// </remarks>
    internal bool IsHeldByOtherWork(ClaimHolderKind holder, Key? partition, JobId? job, ulong? epoch)
    {
        var held = HeldClaim(holder, partition);
        if (held is not { } claim) return false;
        if (job is not null && claim.Job is not null) return claim.Job != job;
        return epoch is not null && epoch != claim.Epoch.Value;
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
    /// Releases the observer-wide claim held by a job of the given kind, leaving a claim granted to other work in place.
    /// </summary>
    /// <param name="holder">The <see cref="ClaimHolderKind"/> handing it over.</param>
    /// <param name="job">The <see cref="JobId"/> of the job handing it over.</param>
    /// <remarks>
    /// A claim not yet attached to a job is released too: the job was started under it but the attachment was not persisted.
    /// </remarks>
    internal void HandOverObserverWide(ClaimHolderKind holder, JobId job)
    {
        // A claim with no job attached yet takes the handing-over job as its own.
        if (_wide?.Holder == holder && (_wide.Job ?? job) == job)
        {
            HandOverObserverWide();
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
        (_orderedClaims ??= _claims.Values.OrderBy(_ => _.Partition.ToString(), StringComparer.Ordinal).ToArray(), _wide, _revision);

    /// <summary>
    /// Checks, without allocating, whether the claims already agree with the authoritative sets.
    /// </summary>
    /// <param name="replaying">The authoritative replaying partitions.</param>
    /// <param name="catchingUp">The authoritative catching-up partitions.</param>
    /// <param name="observerReplaying">Whether the observer is replaying.</param>
    /// <returns>True when <see cref="Compare"/> reports nothing and <see cref="Reconcile"/> changes nothing; false only means the full path runs.</returns>
    internal bool IsAligned(ISet<Key> replaying, ISet<Key> catchingUp, bool observerReplaying)
    {
        if (observerReplaying != (_wide?.Holder == ClaimHolderKind.ObserverReplay))
        {
            return false;
        }

        foreach (var claim in _claims.Values)
        {
            if (claim.Holder is ClaimHolderKind.PartitionReplay or ClaimHolderKind.PartitionCatchUp &&
                claim.Holder != KindFromSets(claim.Partition, replaying, catchingUp))
            {
                return false;
            }
        }

        return AllHaveClaims(replaying) && (_wide?.Holder == ClaimHolderKind.ObserverCatchUp || AllHaveClaims(catchingUp));
    }

    static ClaimHolderKind KindFromSets(Key partition, ISet<Key> replaying, ISet<Key> catchingUp)
    {
        if (replaying.Contains(partition))
        {
            return ClaimHolderKind.PartitionReplay;
        }

        return catchingUp.Contains(partition) ? ClaimHolderKind.PartitionCatchUp : ClaimHolderKind.None;
    }

    bool AllHaveClaims(ISet<Key> partitions)
    {
        if (partitions.Count == 0)
        {
            return true;
        }

        if (partitions is HashSet<Key> hashSet)
        {
            foreach (var partition in hashSet)
            {
                if (!_claims.ContainsKey(partition))
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var partition in partitions)
        {
            if (!_claims.ContainsKey(partition))
            {
                return false;
            }
        }

        return true;
    }

    void SetClaim(Key partition, PartitionClaim claim)
    {
        _claims[partition] = claim;
        _orderedClaims = null;
    }

    bool RemoveClaim(Key partition)
    {
        _orderedClaims = null;
        return _claims.Remove(partition);
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

    (JobId? Job, ClaimEpoch Epoch)? HeldClaim(ClaimHolderKind holder, Key? partition)
    {
        if (partition is null)
        {
            return _wide?.Holder == holder ? (_wide.Job, _wide.Epoch) : null;
        }

        return _claims.TryGetValue(partition, out var claim) && claim.Holder == holder ? (claim.Job, claim.Epoch) : null;
    }
}
