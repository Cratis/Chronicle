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
        if (_differences.Exists(difference => !barrier.IsExcluded(difference) && barrier.IsPending(difference.Name))) return false;
        if (_segment < _segments || _differences.Count > Math.Max(1, keyLimit / 2)) barrier.UnderRefusal = true;
        if (!barrier.UnderRefusal)
        {
            _catchUpGroups ??= PlanCatchUpGroups(budget);
            if (_catchUpGroups.Count == 0 && _differences.Exists(difference => !barrier.IsExcluded(difference))) barrier.UnderRefusal = true;
        }
        if (barrier.UnderRefusal)
        {
            await barrier.Refuse(_differences.Select(difference => difference.Name).Distinct(), budget);
            await ReleaseDifferences(budget);
            if (Interrupted() || budget.IsCancellationRequested) return false;
            await ClaimDifferences(budget);
            if (Interrupted() || budget.IsCancellationRequested) return false;
            if (_differences.Exists(difference => CanWrite(difference) && (!difference.Released || !difference.Claimed))) return false;
            if (_segment >= _segments && (!_needsRetryPass || _retryPass))
            {
                await barrier.Allow(CancellationToken.None);
                barrier.UnderRefusal = false;
            }
        }
        else if (!await WriteCatchUpGroups(budget))
        {
            return false;
        }
        _catchUpGroups = null;
        _catchUpGroupIndex = 0;
        return true;
    }

    async Task<bool> WriteCatchUpGroups(CancellationToken budget)
    {
        var barrier = CatchUp!;
        if (_catchUpGroupIndex >= _catchUpGroups!.Count) return true;
        await barrier.Refuse(_differences.Select(difference => difference.Name).Distinct(), budget);
        while (_catchUpGroupIndex < _catchUpGroups.Count && !budget.IsCancellationRequested)
        {
            var group = _catchUpGroups[_catchUpGroupIndex];

            // Once started, a group finishes independently of the soft turn budget. Each of its
            // at most two writes is attempted once under its own one-second deadline.
            if (group.Release is { } release && !release.Released && CanWrite(release))
            {
                release.Released = await Write(release, release.Release!, CancellationToken.None);
                if (!release.Released)
                {
                    barrier.UnderRefusal = true;
                    return false;
                }
            }
            if (group.Claim is { } claim && !claim.Claimed && CanWrite(claim))
            {
                claim.Claimed = await Write(claim, claim.Claim!, CancellationToken.None);
                if (!claim.Claimed)
                {
                    barrier.UnderRefusal = true;
                    return false;
                }
            }
            _catchUpGroupIndex++;
        }
        if (_catchUpGroups.Exists(group => group.Release?.Released == true && group.Claim is { Claimed: false } claim && CanWrite(claim))) return false;
        await barrier.Allow(CancellationToken.None);
        return _catchUpGroupIndex == _catchUpGroups.Count;
    }

    List<CatchUpGroup> PlanCatchUpGroups(CancellationToken budget)
    {
        var releases = new Dictionary<ValueKey, ConstraintIndexReplayDifference>();
        var claims = new Dictionary<ValueKey, ConstraintIndexReplayDifference>();
        foreach (var difference in _differences.Where(difference => !CatchUp!.IsExcluded(difference)))
        {
            budget.ThrowIfCancellationRequested();
            if (difference.Release is not null)
            {
                if (difference.PreviousValues.Count != 1 || !releases.TryAdd(new(difference.Name, difference.ScopeKey, difference.PreviousValues[0]), difference)) return [];
            }
            else
            {
                difference.Released = true;
            }
            if (difference.Claim is not null && difference.Value is { } value)
            {
                if (!claims.TryAdd(new(difference.Name, difference.ScopeKey, value), difference)) return [];
            }
            else
            {
                difference.Claimed = true;
            }
        }
        var groups = releases.Keys.Concat(claims.Keys.Where(key => !releases.ContainsKey(key))).Select(key => new CatchUpGroup(releases.GetValueOrDefault(key), claims.GetValueOrDefault(key))).ToList();
        var ready = new Queue<CatchUpGroup>();
        var dependants = new Dictionary<ConstraintIndexReplayDifference, CatchUpGroup>();
        foreach (var group in groups)
        {
            budget.ThrowIfCancellationRequested();
            if (group.Claim is not { Release: not null } claim || ReferenceEquals(group.Release, claim)) ready.Enqueue(group);
            else dependants[claim] = group;
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
