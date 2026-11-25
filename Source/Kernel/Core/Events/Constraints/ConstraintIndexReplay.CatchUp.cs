// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    List<CatchUpGroup>? _catchUpGroups;
    int _catchUpGroupIndex;

    /// <summary>
    /// Gets a value indicating whether the last turn could not write because a difference belongs to a constraint pending repair.
    /// </summary>
    internal bool BlockedByPendingRepair { get; private set; }

    /// <summary>
    /// Extends the current reading segment, and invalidates differences collected at the old boundary.
    /// </summary>
    /// <param name="boundary">The new exclusive end point.</param>
    internal void RaiseCatchUpBoundary(EventSequenceNumber boundary)
    {
        if (boundary <= _endPoint) return;
        _endPoint = boundary;
        if (CatchUp is { Refusing: true } barrier) barrier.UnderRefusal = true;
        _differences.Clear();
        _catchUpGroups = null;
        _catchUpGroupIndex = 0;
        if (_phase != ConstraintIndexReplayPhase.Reading)
        {
            _unique.Clear();
            _closed.Clear();
            _segment = 0;
            BeginSegment();
        }
        _remainingCatchUpSegments = new(Enumerable.Range(0, _segments).Where(segment => segment != _segment));
    }

    static bool TryIndexRelease(ConstraintIndexReplayDifference difference, Dictionary<ValueKey, ConstraintIndexReplayDifference> releases)
    {
        if (difference.Release is null)
        {
            difference.Released = true;
            return true;
        }

        return difference.PreviousValues.Count == 1 && releases.TryAdd(new(difference.Name, difference.ScopeKey, difference.PreviousValues[0]), difference);
    }

    static bool TryIndexClaim(ConstraintIndexReplayDifference difference, Dictionary<ValueKey, ConstraintIndexReplayDifference> claims)
    {
        if (difference.Claim is null || difference.Value is not { } value)
        {
            difference.Claimed = true;
            return true;
        }

        return claims.TryAdd(new(difference.Name, difference.ScopeKey, value), difference);
    }

    static bool IsDependant(CatchUpGroup group) => group.Claim is { Release: not null } claim && !ReferenceEquals(group.Release, claim);

    void AdvanceSegment()
    {
        if (_remainingCatchUpSegments is { } remaining)
        {
            _segment = remaining.TryDequeue(out var next) ? next : _segments;
            if (_segment == _segments) _remainingCatchUpSegments = null;
        }
        else
        {
            _segment++;
        }
    }

    async Task<bool> WriteCatchUp(CancellationToken budget)
    {
        var barrier = CatchUp!;
        BlockedByPendingRepair = _differences.Exists(difference => !barrier.IsExcluded(difference) && barrier.IsPending(difference.Name));
        if (BlockedByPendingRepair) return false;
        PlanRefusal(barrier, budget);
        var written = barrier.UnderRefusal ? await WriteUnderRefusal(barrier, budget) : await WriteCatchUpGroups(budget);
        if (!written) return false;
        _catchUpGroups = null;
        _catchUpGroupIndex = 0;
        return true;
    }

    void PlanRefusal(ConstraintIndexCatchUpWriting barrier, CancellationToken budget)
    {
        if (_segment < _segments || _differences.Count > Math.Max(1, keyLimit / 2)) barrier.UnderRefusal = true;
        if (barrier.UnderRefusal) return;

        _catchUpGroups ??= PlanCatchUpGroups(budget);
        if (_catchUpGroups.Count == 0 && _differences.Exists(difference => !barrier.IsExcluded(difference))) barrier.UnderRefusal = true;
    }

    async Task<bool> WriteUnderRefusal(ConstraintIndexCatchUpWriting barrier, CancellationToken budget)
    {
        await barrier.Refuse(_differences.Select(difference => difference.Name).Distinct(), budget);
        await ReleaseDifferences(budget);
        if (StopsTurn(budget)) return false;
        await ClaimDifferences(budget);
        if (StopsTurn(budget)) return false;
        if (_differences.Exists(HasPendingWrite)) return false;
        if (_segment >= _segments && (!_needsRetryPass || _retryPass))
        {
            await barrier.Allow(CancellationToken.None);
            barrier.UnderRefusal = false;
        }

        return true;
    }

    async Task<bool> WriteCatchUpGroups(CancellationToken budget)
    {
        var barrier = CatchUp!;
        if (_catchUpGroupIndex >= _catchUpGroups!.Count) return true;
        await barrier.Refuse(_differences.Select(difference => difference.Name).Distinct(), budget);
        while (_catchUpGroupIndex < _catchUpGroups.Count && !budget.IsCancellationRequested)
        {
            if (!await WriteCatchUpGroup(_catchUpGroups[_catchUpGroupIndex], barrier)) return false;
            _catchUpGroupIndex++;
        }
        if (_catchUpGroups.Exists(HasWritableClaimAfterRelease)) return false;
        await barrier.Allow(CancellationToken.None);
        return _catchUpGroupIndex == _catchUpGroups.Count;
    }

    async Task<bool> WriteCatchUpGroup(CatchUpGroup group, ConstraintIndexCatchUpWriting barrier)
    {
        // Once started, a group finishes independently of the soft turn budget. Each of its
        // at most two writes is attempted once under its own one-second deadline.
        return await WriteGroupRelease(group.Release, barrier) && await WriteGroupClaim(group.Claim, barrier);
    }

    async Task<bool> WriteGroupRelease(ConstraintIndexReplayDifference? difference, ConstraintIndexCatchUpWriting barrier)
    {
        if (difference is not { } release || release.Released || !CanWrite(release)) return true;

        release.Released = await Write(release, release.Release!, CancellationToken.None);
        if (release.Released) return true;

        barrier.UnderRefusal = true;
        return false;
    }

    async Task<bool> WriteGroupClaim(ConstraintIndexReplayDifference? difference, ConstraintIndexCatchUpWriting barrier)
    {
        if (difference is not { } claim || claim.Claimed || !CanWrite(claim)) return true;

        claim.Claimed = await Write(claim, claim.Claim!, CancellationToken.None);
        if (claim.Claimed) return true;

        barrier.UnderRefusal = true;
        return false;
    }

    bool StopsTurn(CancellationToken budget) => Interrupted() || budget.IsCancellationRequested;

    bool HasPendingWrite(ConstraintIndexReplayDifference difference) => CanWrite(difference) && (!difference.Released || !difference.Claimed);

    bool HasWritableClaimAfterRelease(CatchUpGroup group) => group.Release?.Released == true && group.Claim is { Claimed: false } claim && CanWrite(claim);

    List<CatchUpGroup> PlanCatchUpGroups(CancellationToken budget)
    {
        if (!TryIndexCatchUpWrites(budget, out var releases, out var claims)) return [];

        var groups = releases.Keys.Concat(claims.Keys.Where(key => !releases.ContainsKey(key))).Select(key => new CatchUpGroup(releases.GetValueOrDefault(key), claims.GetValueOrDefault(key))).ToList();
        return OrderCatchUpGroups(groups, budget);
    }

    bool TryIndexCatchUpWrites(
        CancellationToken budget,
        out Dictionary<ValueKey, ConstraintIndexReplayDifference> releases,
        out Dictionary<ValueKey, ConstraintIndexReplayDifference> claims)
    {
        releases = [];
        claims = [];
        foreach (var difference in _differences.Where(difference => !CatchUp!.IsExcluded(difference)))
        {
            budget.ThrowIfCancellationRequested();
            if (!TryIndexRelease(difference, releases) || !TryIndexClaim(difference, claims)) return false;
        }

        return true;
    }

    List<CatchUpGroup> OrderCatchUpGroups(List<CatchUpGroup> groups, CancellationToken budget)
    {
        var ready = new Queue<CatchUpGroup>();
        var dependants = new Dictionary<ConstraintIndexReplayDifference, CatchUpGroup>();
        foreach (var group in groups)
        {
            budget.ThrowIfCancellationRequested();
            if (!IsDependant(group)) ready.Enqueue(group);
            else dependants[group.Claim!] = group;
        }
        var ordered = new List<CatchUpGroup>(groups.Count);
        while (ready.TryDequeue(out var group))
        {
            budget.ThrowIfCancellationRequested();
            ordered.Add(group);
            if (group.Release is { } release && dependants.Remove(release, out var dependant)) ready.Enqueue(dependant);
        }
        return ordered.Count == groups.Count ? ordered : [];
    }

    [StructLayout(LayoutKind.Auto)]
    readonly record struct ValueKey(ConstraintName Name, string Scope, UniqueConstraintValue Value);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct CatchUpGroup(ConstraintIndexReplayDifference? Release, ConstraintIndexReplayDifference? Claim);
}
