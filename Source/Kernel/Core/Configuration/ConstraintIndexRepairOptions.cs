// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration of constraint-index repair: the retries, time budgets, cadence and memory caps
/// that bound how an event sequence keeps its constraint indexes in step with the event log.
/// </summary>
/// <remarks>
/// <para>
/// The defaults are the values accepted in decision 0008. They are sized against the 30-second
/// <c language="csharp">clustering.responseTimeout</c>: the worst case for one append turn is approximately
/// <see cref="TimerTurnBudget"/> + 2 × <see cref="AttemptTimeout"/> (a catch-up write turn may overrun) +
/// <see cref="AppendRepairBudget"/> + 2 × <see cref="PostCommitBudget"/> + 2 × 2 seconds of read-back.
/// With the defaults that is 2 + 2 + 5 + 4 + 4 = 17 seconds. When lowering the response timeout or raising a
/// budget, keep that sum well below the response timeout. The sum is not checked at startup, so a deployment
/// with a short response timeout keeps working.
/// </para>
/// </remarks>
public class ConstraintIndexRepairOptions
{
    /// <summary>
    /// The default for <see cref="FoldKeyLimit"/>, usable as a compile-time constant.
    /// </summary>
    internal const int DefaultFoldKeyLimit = 100000;

    const string SettingPrefix = "ConstraintIndexRepair";
    static readonly TimeSpan _minimumTurnBudget = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets the total number of in-turn attempts, the first try included, to write a constraint index update.
    /// </summary>
    /// <remarks>
    /// An update that still fails after this many attempts is left to repair from the event log. Each retry adds
    /// delay to the append, so raising it trades append latency for fewer repairs. Must be between 1 and 10.
    /// Defaults to 3.
    /// </remarks>
    public int UpdateAttempts { get; init; } = 3;

    /// <summary>
    /// Gets the longest a single index write may run before it is abandoned.
    /// </summary>
    /// <remarks>
    /// Bounds one write independently of its enclosing turn, so a stuck storage call cannot consume the whole
    /// turn. Lower it for fast storage; raise it for slow or distant storage. Must be greater than zero.
    /// Defaults to 1 second.
    /// </remarks>
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the time budget for updating the constraint indexes after an append has committed.
    /// </summary>
    /// <remarks>
    /// When the budget is spent the remaining work is left to repair. Must be greater than zero.
    /// Defaults to 2 seconds.
    /// </remarks>
    public TimeSpan PostCommitBudget { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets the time budget for one timer-driven repair or catch-up turn.
    /// </summary>
    /// <remarks>
    /// The catch-up turn runs for this budget minus a 250 millisecond soft margin. Must be at least
    /// 500 milliseconds. Defaults to 2 seconds.
    /// </remarks>
    public TimeSpan TimerTurnBudget { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets the time budget for a repair performed in the turn of an append.
    /// </summary>
    /// <remarks>
    /// This time is added to the append that triggers the repair, so raising it trades append latency for
    /// faster convergence. It is also the budget of the repair that follows a rebuild. Must be at least
    /// 500 milliseconds. Defaults to 5 seconds.
    /// </remarks>
    public TimeSpan AppendRepairBudget { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the minimum time between repairs started by appends.
    /// </summary>
    /// <remarks>
    /// A value of <see cref="TimeSpan.Zero"/> lets every append turn repair. Must not be negative.
    /// Defaults to 5 seconds.
    /// </remarks>
    public TimeSpan AppendRepairInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the number of times the same failure at the same position must repeat before it is settled as poison.
    /// </summary>
    /// <remarks>
    /// Lower values stop retrying an event that can never be applied sooner, at the risk of settling a failure
    /// that would have cleared. Must be at least 2. Defaults to 5.
    /// </remarks>
    public int PoisonThreshold { get; init; } = 5;

    /// <summary>
    /// Gets the maximum number of staged keys and differences a single fold may retain.
    /// </summary>
    /// <remarks>
    /// Caps the memory of a fold; a fold that reaches it yields and continues in a later turn. Must be at least 1.
    /// Defaults to 100000.
    /// </remarks>
    public int FoldKeyLimit { get; init; } = DefaultFoldKeyLimit;

    /// <summary>
    /// Gets the maximum number of excluded hashes a catch-up retains before its boundary is raised.
    /// </summary>
    /// <remarks>
    /// Caps the memory of the catch-up exclusion set. Must be at least 1. Defaults to 100000.
    /// </remarks>
    public int CatchUpExclusionLimit { get; init; } = 100000;

    /// <summary>
    /// Gets the number of catch-ups that may run concurrently in a silo.
    /// </summary>
    /// <remarks>
    /// Applies to both the executing turns and the memory retained between turns. Raise it only when the silo has
    /// memory to spare. Must be at least 1. Defaults to 4.
    /// </remarks>
    public int CatchUpSlots { get; init; } = 4;

    /// <summary>
    /// Gets the shared default instance, used where no configured options are available.
    /// </summary>
    /// <remarks>
    /// Safe to share because every property is init-only.
    /// </remarks>
    internal static ConstraintIndexRepairOptions Default { get; } = new();

    /// <summary>
    /// Validates the configured values.
    /// </summary>
    /// <returns>The validation errors; empty when every value is in range.</returns>
    public IEnumerable<ValidationResult> Validate() => ValidateTimes().Concat(ValidateCounts());

    static ValidationResult Error(string name, string rule) =>
        new($"{SettingPrefix}.{name} {rule}.", [$"{SettingPrefix}.{name}"]);

    IEnumerable<ValidationResult> ValidateTimes()
    {
        if (AttemptTimeout <= TimeSpan.Zero) yield return Error(nameof(AttemptTimeout), "must be greater than zero");
        if (PostCommitBudget <= TimeSpan.Zero) yield return Error(nameof(PostCommitBudget), "must be greater than zero");
        if (TimerTurnBudget < _minimumTurnBudget) yield return Error(nameof(TimerTurnBudget), "must be at least 500 milliseconds");
        if (AppendRepairBudget < _minimumTurnBudget) yield return Error(nameof(AppendRepairBudget), "must be at least 500 milliseconds");
        if (AppendRepairInterval < TimeSpan.Zero) yield return Error(nameof(AppendRepairInterval), "must not be negative");
    }

    IEnumerable<ValidationResult> ValidateCounts()
    {
        if (UpdateAttempts is < 1 or > 10) yield return Error(nameof(UpdateAttempts), "must be between 1 and 10");
        if (PoisonThreshold < 2) yield return Error(nameof(PoisonThreshold), "must be at least 2");
        if (FoldKeyLimit < 1) yield return Error(nameof(FoldKeyLimit), "must be at least 1");
        if (CatchUpExclusionLimit < 1) yield return Error(nameof(CatchUpExclusionLimit), "must be at least 1");
        if (CatchUpSlots < 1) yield return Error(nameof(CatchUpSlots), "must be at least 1");
    }
}
