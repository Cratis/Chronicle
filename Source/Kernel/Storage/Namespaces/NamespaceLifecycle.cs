// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Durable control state stored outside the namespace database. Revisions never reset when a name is reused.
/// </summary>
/// <param name="Namespace">The canonical namespace name.</param>
/// <param name="Incarnation">The physical namespace lifetime.</param>
/// <param name="Created">When this incarnation was reserved.</param>
public record NamespaceLifecycle(EventStoreNamespaceName Namespace, NamespaceIncarnationId Incarnation, DateTimeOffset Created)
{
    /// <summary>The maximum automatic attempts for one removal operation.</summary>
    public const int MaximumAttempts = 5;

    /// <summary>Gets the compare-and-swap revision.</summary>
    public long Revision { get; init; } = 1;

    /// <summary>Gets the retired marker identity that a prepared replacement may supersede.</summary>
    public NamespaceIncarnationId? PreviousIncarnation { get; init; }

    /// <summary>Gets whether creation notification and listing acknowledgement are still required before opening.</summary>
    public bool NotifyOnOpen { get; init; }

    /// <summary>Gets the lifecycle status.</summary>
    public NamespaceLifecycleStatus Status { get; init; } = NamespaceLifecycleStatus.Preparing;

    /// <summary>Gets the removal operation identity.</summary>
    public Guid OperationId { get; init; }

    /// <summary>Gets the next unacknowledged removal checkpoint.</summary>
    public NamespaceRemovalStage Stage { get; init; }

    /// <summary>Gets the consumed attempt budget for this checkpoint.</summary>
    public int StageAttempts { get; init; }

    /// <summary>Gets the total attempts consumed by this operation.</summary>
    public int TotalAttempts { get; init; }

    /// <summary>Gets the historical attempt count at the start of the latest operator-authorized budget.</summary>
    public int BudgetStartAttempts { get; init; }

    /// <summary>Gets whether an attempt was recorded without an acknowledged completion.</summary>
    public bool AttemptInProgress { get; init; }

    /// <summary>Gets whether a destructive stage has an unknown terminal outcome.</summary>
    public bool DestructiveAttemptOutstanding { get; init; }

    /// <summary>Gets whether any destructive effect, including Forget or observer cleanup, could have been issued.</summary>
    public bool DestructiveActionIssued { get; init; }

    /// <summary>Gets the last attempt time.</summary>
    public DateTimeOffset? LastAttemptAt { get; init; }

    /// <summary>Gets the earliest automatic retry time.</summary>
    public DateTimeOffset? RetryAt { get; init; }

    /// <summary>Gets the current attempt's observation deadline. Expiry never proves its effects stopped.</summary>
    public DateTimeOffset? Deadline { get; init; }

    /// <summary>Gets the last stable diagnostic code, never an exception message.</summary>
    public string LastFailure { get; init; } = string.Empty;

    /// <summary>Creates an already initialized lifecycle.</summary>
    /// <param name="name">The canonical name.</param>
    /// <param name="incarnation">The installed marker identity.</param>
    /// <param name="now">The creation time.</param>
    /// <returns>The open lifecycle.</returns>
    public static NamespaceLifecycle Open(EventStoreNamespaceName name, NamespaceIncarnationId incarnation, DateTimeOffset now) =>
        new(name, incarnation, now) { Status = NamespaceLifecycleStatus.Open };

    /// <summary>Closes admission before any removal effect is issued.</summary>
    /// <param name="operationId">The new removal operation.</param>
    /// <param name="now">The operation start time.</param>
    /// <returns>The next control state.</returns>
    /// <exception cref="InvalidOperationException">The incarnation is not open or the operation identity is unspecified.</exception>
    public NamespaceLifecycle BeginRemoval(Guid operationId, DateTimeOffset now)
    {
        if (Status != NamespaceLifecycleStatus.Open || operationId == Guid.Empty)
        {
            throw new InvalidOperationException("Only an open incarnation may begin a new removal operation.");
        }
        return this with
        {
            Revision = Revision + 1,
            Status = NamespaceLifecycleStatus.Closing,
            OperationId = operationId,
            Stage = NamespaceRemovalStage.CloseMarker,
            RetryAt = now
        };
    }

    /// <summary>Consumes an attempt before issuing its effect.</summary>
    /// <param name="now">The attempt time.</param>
    /// <returns>The state which must be acknowledged by compare-and-swap before issuing the effect.</returns>
    /// <exception cref="InvalidOperationException">The operation is not closing or already has an outstanding attempt.</exception>
    public NamespaceLifecycle BeginAttempt(DateTimeOffset now)
    {
        if (Status != NamespaceLifecycleStatus.Closing || AttemptInProgress)
        {
            throw new InvalidOperationException("An attempt requires an idle closing operation.");
        }
        if (TotalAttempts - BudgetStartAttempts >= MaximumAttempts)
        {
            return RequireRecovery("NamespaceRemovalBudgetExhausted");
        }
        return this with
        {
            Revision = Revision + 1,
            StageAttempts = StageAttempts + 1,
            TotalAttempts = TotalAttempts + 1,
            LastAttemptAt = now,
            Deadline = now.AddSeconds(30),
            RetryAt = null,
            AttemptInProgress = true,
            DestructiveActionIssued = DestructiveActionIssued || Stage >= NamespaceRemovalStage.Forget,
            DestructiveAttemptOutstanding = Stage >= NamespaceRemovalStage.Forget
        };
    }

    /// <summary>Records the next stage's intent before its effect is issued within the current attempt.</summary>
    /// <returns>The state which must be acknowledged before issuing the stage.</returns>
    /// <exception cref="InvalidOperationException">There is no active attempt or an effect is already outstanding.</exception>
    public NamespaceLifecycle IssueStage()
    {
        if (!AttemptInProgress || DestructiveAttemptOutstanding)
        {
            throw new InvalidOperationException("A stage requires an active attempt without an outstanding destructive effect.");
        }
        return this with
        {
            Revision = Revision + 1,
            DestructiveActionIssued = DestructiveActionIssued || Stage >= NamespaceRemovalStage.Forget,
            DestructiveAttemptOutstanding = Stage >= NamespaceRemovalStage.Forget
        };
    }

    /// <summary>Records an acknowledged checkpoint completion.</summary>
    /// <param name="now">The completion time.</param>
    /// <returns>The next checkpoint, or the final tombstone.</returns>
    /// <exception cref="InvalidOperationException">The operation has no outstanding closing attempt.</exception>
    public NamespaceLifecycle CompleteStage(DateTimeOffset now)
    {
        if (Status != NamespaceLifecycleStatus.Closing || !AttemptInProgress)
        {
            throw new InvalidOperationException("Only an outstanding attempt may complete.");
        }
        var next = Stage + 1;
        return this with
        {
            Revision = Revision + 1,
            Stage = next,
            Status = next == NamespaceRemovalStage.Complete ? NamespaceLifecycleStatus.Removed : Status,
            StageAttempts = 0,
            AttemptInProgress = next != NamespaceRemovalStage.Complete,
            DestructiveAttemptOutstanding = false,
            RetryAt = now,
            LastFailure = string.Empty
        };
    }

    /// <summary>Records a terminal failed attempt without refunding its budget.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="now">The terminal failure time.</param>
    /// <returns>The retryable or recovery-required control state.</returns>
    public NamespaceLifecycle FailAttempt(string code, DateTimeOffset now)
    {
        var failed = this with
        {
            Revision = Revision + 1,
            AttemptInProgress = false,
            DestructiveAttemptOutstanding = false,
            RetryAt = now.AddMinutes(1),
            LastFailure = code
        };
        return TotalAttempts - BudgetStartAttempts >= MaximumAttempts
            ? failed with { Status = NamespaceLifecycleStatus.RecoveryRequired }
            : failed;
    }

    /// <summary>Recovers activation without reissuing an unknown destructive operation.</summary>
    /// <param name="now">The recovery time.</param>
    /// <returns>The recovered control state with consumed budgets preserved.</returns>
    public NamespaceLifecycle RecoverOutstandingAttempt(DateTimeOffset now)
    {
        if (!AttemptInProgress) return this;
        if (DestructiveAttemptOutstanding) return RequireRecovery("NamespaceRemovalOutcomeUnknown");
        return this with { Revision = Revision + 1, AttemptInProgress = false, RetryAt = now };
    }

    /// <summary>Stops automatic recovery while preserving all operation identity and attempt metadata.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <returns>The recovery-required control state.</returns>
    public NamespaceLifecycle RequireRecovery(string code) => this with
    {
        Revision = Revision + 1,
        Status = NamespaceLifecycleStatus.RecoveryRequired,
        LastFailure = code
    };
}
