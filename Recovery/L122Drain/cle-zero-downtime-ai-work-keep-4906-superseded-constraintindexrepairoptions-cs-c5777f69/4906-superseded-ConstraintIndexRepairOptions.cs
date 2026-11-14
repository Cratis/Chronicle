// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents timing options for repairing constraint indexes from the event log.
/// </summary>
public class ConstraintIndexRepairOptions : IValidatableObject
{
    /// <summary>
    /// Gets the minimum interval between append-triggered repairs, measured from the end of the previous repair.
    /// </summary>
    public TimeSpan AppendRepairInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the hard time budget for an append-triggered repair.
    /// </summary>
    public TimeSpan AppendRepairBudget { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the initial delay and period of the constraint repair timer.
    /// </summary>
    public TimeSpan TimerInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the hard time budget for a timer-triggered repair.
    /// </summary>
    public TimeSpan TimerRepairBudget { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets the time budget for updating constraint indexes after an append commits.
    /// </summary>
    public TimeSpan PostCommitUpdateBudget { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets the time reserved at the end of a repair's hard budget for completing the turn.
    /// </summary>
    public TimeSpan SoftBudgetMargin { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Gets the snapshot write budget, further bounded by the remaining hard repair budget.
    /// </summary>
    public TimeSpan SnapshotBudget { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Gets the time budget for a catch-up turn, independent of the Orleans response timeout.
    /// </summary>
    public TimeSpan CatchUpBudget { get; init; } = TimeSpan.FromMilliseconds(1750);

    /// <summary>
    /// Gets the maximum outer repair retry delay. Retries start at one second and double up to this cap.
    /// </summary>
    public TimeSpan RetryMaxDelay { get; init; } = TimeSpan.FromSeconds(60);

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var durations = new Dictionary<string, TimeSpan>
        {
            [nameof(AppendRepairInterval)] = AppendRepairInterval,
            [nameof(AppendRepairBudget)] = AppendRepairBudget,
            [nameof(TimerInterval)] = TimerInterval,
            [nameof(TimerRepairBudget)] = TimerRepairBudget,
            [nameof(PostCommitUpdateBudget)] = PostCommitUpdateBudget,
            [nameof(SoftBudgetMargin)] = SoftBudgetMargin,
            [nameof(SnapshotBudget)] = SnapshotBudget,
            [nameof(CatchUpBudget)] = CatchUpBudget,
            [nameof(RetryMaxDelay)] = RetryMaxDelay
        };
        foreach (var (name, duration) in durations)
        {
            // TimeProvider timers and CancellationTokenSource support at most uint.MaxValue - 1 milliseconds.
            if (duration <= TimeSpan.Zero || duration > TimeSpan.FromMilliseconds(uint.MaxValue - 1))
            {
                yield return new ValidationResult($"{name} must be positive and within the supported timer range.", [name]);
            }
        }
        foreach (var result in ValidateRelationships()) yield return result;
    }

    IEnumerable<ValidationResult> ValidateRelationships()
    {
        if (SoftBudgetMargin >= AppendRepairBudget)
        {
            yield return new ValidationResult("SoftBudgetMargin must be less than AppendRepairBudget.", [nameof(SoftBudgetMargin)]);
        }
        if (SoftBudgetMargin >= TimerRepairBudget)
        {
            yield return new ValidationResult("SoftBudgetMargin must be less than TimerRepairBudget.", [nameof(SoftBudgetMargin)]);
        }
        if (SnapshotBudget > SoftBudgetMargin)
        {
            yield return new ValidationResult("SnapshotBudget must not exceed SoftBudgetMargin.", [nameof(SnapshotBudget)]);
        }
        if (TimerRepairBudget > TimeSpan.Zero && SoftBudgetMargin > TimeSpan.Zero && CatchUpBudget > TimerRepairBudget - SoftBudgetMargin)
        {
            yield return new ValidationResult("CatchUpBudget must not exceed TimerRepairBudget minus SoftBudgetMargin.", [nameof(CatchUpBudget)]);
        }
    }
}
