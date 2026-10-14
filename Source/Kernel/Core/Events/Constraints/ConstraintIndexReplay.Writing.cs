// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    async Task CollectDifferences(CancellationToken cancellationToken)
    {
        while (_unique.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (key, state) = _unique.First();
            var current = (await unique.GetFor(key.Source, state.Definition, key.Scope, cancellationToken)).ToArray();
            _unique.Remove(key);
            var retained = key.Retained;
            var matches = state.Value is null ? current.Length == 0 : current.Length == 1 && current[0] == state.Value;
            if (retained is not null) matches = current.Contains(retained) == (state.Value is not null);
            if (!matches)
            {
                Func<CancellationToken, Task>? release = null;
                if (retained is not null && current.Contains(retained))
                {
                    release = token => unique.RemoveValue(key.Source, state.Definition, retained, key.Scope, token);
                }
                else if (retained is null && current.Length > 0)
                {
                    release = token => unique.Remove(key.Source, state.Definition, key.Scope, token);
                }
                Func<CancellationToken, Task>? claim = state.Value is { } value
                    ? token => unique.Save(key.Source, state.Definition, state.Position, value, key.Scope, token)
                    : null;
                _differences.Add(new(key.Name, state.Position, release, claim, key.Scope, state.Value, retained is null ? current : current.Where(value => value == retained).ToArray()));
            }
            TrackKeys();
        }
        while (_closed.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (key, state) = _closed.First();
            var current = (await closed.GetForOwner(key.Name.Value, cancellationToken)).SingleOrDefault(row => row.Scope.Normalized() == key.Scope);
            _closed.Remove(key);
            if (current != state.Closure)
            {
                Func<CancellationToken, Task> claim = state.Closure is { } closure
                    ? token => closed.Close(closure, token)
                    : token => closed.Reopen(key.Name.Value, key.Scope, token);
                _differences.Add(new(key.Name, state.Position, null, claim, string.Empty, null, []));
            }
            TrackKeys();
        }
    }

    async Task ReleaseDifferences(CancellationToken cancellationToken)
    {
        foreach (var difference in _differences.Where(difference => !difference.Released))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_failed.Contains(difference.Name)) continue;
            if (difference.Release is null)
            {
                difference.Released = true;
                continue;
            }
            difference.Released = await Write(difference, difference.Release, cancellationToken);
        }
    }

    async Task ClaimDifferences(CancellationToken cancellationToken)
    {
        foreach (var difference in _differences.Where(difference => !difference.Claimed).OrderBy(difference => difference.Position))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_failed.Contains(difference.Name)) continue;
            if (difference.Claim is null)
            {
                difference.Claimed = true;
                continue;
            }
            if (difference.Retry)
            {
                difference.Claimed = await Write(difference, difference.Claim, cancellationToken);
                continue;
            }
            var (outcome, error) = await ConstraintIndexUpdateAttempt.Run(difference.Claim, cancellationToken);
            if (outcome == ConstraintIndexUpdateOutcome.Settled && !_retryPass &&
                (_segment < _segments || _differences.Exists(holder => !ReferenceEquals(holder, difference) && holder.Name == difference.Name && holder.ScopeKey == difference.ScopeKey && difference.Value is not null && (holder.PreviousValues.Contains(difference.Value) || holder.Value == difference.Value))))
            {
                if (_segment < _segments)
                {
                    // A remaining segment may release this value. Do not settle its claim prematurely.
                    _needsRetryPass = true;
                    difference.Claimed = true;
                }
                else
                {
                    difference.Retry = true;
                }
                continue;
            }
            difference.Claimed = Record(difference, outcome, error);
        }
        foreach (var difference in _differences.Where(difference => difference.Retry && !difference.Claimed && !_failed.Contains(difference.Name)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            difference.Claimed = await Write(difference, difference.Claim!, cancellationToken);
        }
    }

    async Task<bool> Write(ConstraintIndexReplayDifference difference, Func<CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        var (outcome, error) = await ConstraintIndexUpdateAttempt.Run(write, cancellationToken);
        return Record(difference, outcome, error);
    }

    bool Record(ConstraintIndexReplayDifference difference, ConstraintIndexUpdateOutcome outcome, Exception? error)
    {
        switch (outcome)
        {
            case ConstraintIndexUpdateOutcome.Applied:
                _applied++;
                return true;
            case ConstraintIndexUpdateOutcome.Settled:
                _settled++;
                return true;
            case ConstraintIndexUpdateOutcome.SettledWithError:
                _settledWithError++;
                logger.UnresolvedReplayEvent(difference.Name, difference.Position, error!);
                return true;
            default:
                _failed.Add(difference.Name);
                logger.FailedReplayWrite(difference.Name, difference.Position, error!);
                return false;
        }
    }
}
