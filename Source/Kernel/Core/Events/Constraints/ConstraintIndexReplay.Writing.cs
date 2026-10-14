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
            if (MustRetry(difference, outcome))
            {
                if (_segment < _segments)
                {
                    // A remaining segment may release this value. The fixed range retains its final claim
                    // for a bounded retry re-fold; completed keys produce no further write.
                    _needsRetryPass = true;
                    difference.Claimed = true;
                }
                else
                {
                    difference.Retry = true;
                }
                continue;
            }
            difference.Claimed = Record(difference, outcome, error, cancellationToken);
        }
        foreach (var difference in _differences.Where(difference => difference.Retry && !difference.Claimed && CanWrite(difference)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            difference.Claimed = await Write(difference, difference.Claim!, cancellationToken);
        }
    }

    bool CanWrite(ConstraintIndexReplayDifference difference) => !_failed.Contains(difference.Name) && !_abandoned.Contains(difference.Name);

    bool MustRetry(ConstraintIndexReplayDifference difference, ConstraintIndexUpdateOutcome outcome) =>
        outcome == ConstraintIndexUpdateOutcome.Settled && !_retryPass && (_segment < _segments || _differences.Exists(holder => ChangesHolder(holder, difference)));

    bool ChangesHolder(ConstraintIndexReplayDifference holder, ConstraintIndexReplayDifference claimant) =>
        !ReferenceEquals(holder, claimant) && holder.Name == claimant.Name && holder.ScopeKey == claimant.ScopeKey && claimant.Value is not null &&
        (holder.PreviousValues.Contains(claimant.Value) || holder.Value == claimant.Value);

    async Task<bool> Write(ConstraintIndexReplayDifference difference, Func<CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        var (outcome, error) = await ConstraintIndexUpdateAttempt.Run(write, cancellationToken);
        return Record(difference, outcome, error, cancellationToken);
    }

    bool Record(ConstraintIndexReplayDifference difference, ConstraintIndexUpdateOutcome outcome, Exception? error, CancellationToken cancellationToken)
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
            case ConstraintIndexUpdateOutcome.NotAttempted:
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            case ConstraintIndexUpdateOutcome.Abandoned:
                _abandoned.Add(difference.Name);
                _error ??= error;
                _failingPosition ??= difference.Position;
                return false;
            default:
                Fail(difference.Name, difference.Position, error!);
                return false;
        }
    }
}
