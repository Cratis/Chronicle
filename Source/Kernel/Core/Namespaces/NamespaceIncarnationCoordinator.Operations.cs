// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

public sealed partial class NamespaceIncarnationCoordinator
{
    /// <summary>Authorizes another bounded budget without changing operation or incarnation identity.</summary>
    /// <param name="operation">The observed operation identity.</param>
    /// <param name="revision">The observed revision.</param>
    /// <returns>The acknowledged closing state.</returns>
    /// <exception cref="NamespaceRecoveryRequired">An outstanding effect must first be reconciled to a terminal outcome.</exception>
    public async Task<NamespaceLifecycle> Resume(Guid operation, long revision)
    {
        var current = await RecoveryState(operation, revision);
        if (HasOutstandingAttempt(current)) throw new NamespaceRecoveryRequired(name);
        var next = current with
        {
            Revision = current.Revision + 1,
            Status = NamespaceLifecycleStatus.Closing,
            BudgetStartAttempts = current.TotalAttempts,
            StageAttempts = 0,
            RetryAt = clock.GetUtcNow(),
            Deadline = null
        };
        await effects.EnsureWake(next.Revision);
        return await Save(current, next);
    }

    /// <summary>Aborts only pre-destructive removal, replacing the retired lifetime rather than reopening it.</summary>
    /// <param name="operation">The observed operation identity.</param>
    /// <param name="revision">The observed revision.</param>
    /// <returns>The acknowledged replacement, ready for the ordinary initialization protocol.</returns>
    /// <exception cref="NamespaceRemovalCannotBeAborted">An effect is in progress, destructive, or has an unknown outcome.</exception>
    /// <exception cref="NamespaceRecoveryRequired">The retired marker cannot be acknowledged closed.</exception>
    public async Task<NamespaceLifecycle> Abort(Guid operation, long revision)
    {
        var current = await RecoveryState(operation, revision);
        RequireAbortable(current);
        if (!await marker.TryClose(current.Incarnation)) throw new NamespaceRecoveryRequired(name);
        var next = new NamespaceLifecycle(current.Namespace, new NamespaceIncarnationId(Guid.NewGuid()), clock.GetUtcNow())
        {
            Revision = current.Revision + 1,
            PreviousIncarnation = current.Incarnation,
            OperationId = current.OperationId,
            TotalAttempts = current.TotalAttempts,
            BudgetStartAttempts = current.TotalAttempts,
            LastAttemptAt = current.LastAttemptAt,
            LastFailure = "NamespaceRemovalAborted"
        };
        await effects.EnsureWake(next.Revision);
        return await Save(current, next);
    }

    static bool HasOutstandingAttempt(NamespaceLifecycle current) => current.AttemptInProgress || current.DestructiveAttemptOutstanding;

    static void RequireAbortable(NamespaceLifecycle current)
    {
        if (HasOutstandingAttempt(current) || current.DestructiveActionIssued) throw new NamespaceRemovalCannotBeAborted();
    }

    async Task<NamespaceLifecycle> RecoveryState(Guid operation, long revision)
    {
        var current = await lifecycle.Get(name) ?? throw new NamespaceLifecycleChanged(name);
        RequireRecoveryOperation(current, operation, revision);
        return current;
    }

    void RequireRecoveryOperation(NamespaceLifecycle current, Guid operation, long revision)
    {
        if (operation == Guid.Empty) throw new NamespaceLifecycleChanged(name);
        if (current.Status != NamespaceLifecycleStatus.RecoveryRequired) throw new NamespaceLifecycleChanged(name);
        if (current.OperationId != operation) throw new NamespaceLifecycleChanged(name);
        if (current.Revision != revision) throw new NamespaceLifecycleChanged(name);
    }
}
