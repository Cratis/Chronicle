// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    async Task ReleaseDifferences(CancellationToken cancellationToken)
    {
        foreach (var difference in _differences.Where(difference => !difference.Released && CanWrite(difference)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            difference.Released = difference.Release is null || await Write(difference, difference.Release, cancellationToken);
        }
    }

    async Task ClaimDifferences(CancellationToken cancellationToken)
    {
        var decidingOrder = _differences.Where(difference => !difference.Claimed).OrderBy(difference => difference.Position).ToArray();

        // Eligibility must be evaluated after sorting, as each attempt can stop subsequent writes for its constraint.
        foreach (var difference in decidingOrder.Where(CanWrite))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ClaimDifference(difference, cancellationToken);
        }
        foreach (var difference in _differences.Where(difference => difference.Retry && !difference.Claimed && CanWrite(difference)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            difference.Claimed = await Write(difference, difference.Claim!, cancellationToken);
        }
    }

    async Task ClaimDifference(ConstraintIndexReplayDifference difference, CancellationToken cancellationToken)
    {
        if (difference.Claim is null)
        {
            difference.Claimed = true;
            return;
        }
        if (difference.Retry)
        {
            difference.Claimed = await Write(difference, difference.Claim, cancellationToken);
            return;
        }
        WritingConstraint = difference.Name;
        var (outcome, error) = await ConstraintIndexUpdateAttempt.Run(difference.Claim, cancellationToken, ConstraintIndexTimeProvider);
        WritingConstraint = null;
        if (MustRetry(difference, outcome))
        {
            DeferClaim(difference);
            return;
        }
        difference.Claimed = Record(difference, outcome, error, cancellationToken);
    }

    void DeferClaim(ConstraintIndexReplayDifference difference)
    {
        if (_segment >= _segments)
        {
            difference.Retry = true;
            return;
        }

        // A remaining segment may release this value. The fixed range retains its final claim
        // for a bounded retry re-fold; completed keys produce no further write.
        _needsRetryPass = true;
        difference.Claimed = true;
    }

    bool CanWrite(ConstraintIndexReplayDifference difference) => !IsPoison(difference.Name, difference.Position) && !_failed.ContainsKey(difference.Name) && !_abandoned.Contains(difference.Name) &&
        (CatchUp is null || (!CatchUp.IsExcluded(difference) && !CatchUp.IsPending(difference.Name)));

    bool MustRetry(ConstraintIndexReplayDifference difference, ConstraintIndexUpdateOutcome outcome) =>
        outcome == ConstraintIndexUpdateOutcome.Settled && !_retryPass && (_segment < _segments || _differences.Exists(holder => ChangesHolder(holder, difference)));

    bool ChangesHolder(ConstraintIndexReplayDifference holder, ConstraintIndexReplayDifference claimant) =>
        !ReferenceEquals(holder, claimant) && holder.Name == claimant.Name && holder.ScopeKey == claimant.ScopeKey && claimant.Value is not null &&
        (holder.PreviousValues.Contains(claimant.Value) || holder.Value == claimant.Value);

    async Task<bool> Write(ConstraintIndexReplayDifference difference, Func<CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        WritingConstraint = difference.Name;
        var (outcome, error) = await ConstraintIndexUpdateAttempt.Run(write, cancellationToken, ConstraintIndexTimeProvider);
        WritingConstraint = null;
        return Record(difference, outcome, error, cancellationToken);
    }

    bool Record(ConstraintIndexReplayDifference difference, ConstraintIndexUpdateOutcome outcome, Exception? error, CancellationToken cancellationToken)
    {
        if (outcome is ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.NotAttempted) cancellationToken.ThrowIfCancellationRequested();
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
            case ConstraintIndexUpdateOutcome.NotAttempted:
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            case ConstraintIndexUpdateOutcome.Abandoned:
                _abandoned.Add(difference.Name);
                logger.AbandonedReplayWrite(difference.Name, difference.Position, error!);
                return false;
            default:
                _failed.TryAdd(difference.Name, new(difference.Position, error!));
                logger.FailedReplayWrite(difference.Name, difference.Position, error!);
                return false;
        }
    }
}
