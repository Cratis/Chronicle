// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Drives persisted removal checkpoints without reissuing destructive work whose outcome is unknown.
/// The owner must install its durable wake before persisting Closing and call this driver on activation and wake.
/// </summary>
/// <param name="storage">The lifecycle storage outside the namespace database.</param>
/// <param name="execute">The effect for one checkpoint; success must acknowledge the underlying operation, not just its dispatch.</param>
/// <param name="clock">The clock used for the persisted, whole-attempt deadline.</param>
/// <remarks>
/// A deadline bounds observation, not the underlying operation. A timeout or destructive failure preserves the outstanding
/// intent and stops automatic recovery. Neither a late reply nor reactivation is permission to retry or recreate the namespace;
/// an operator must reconcile the provider's outcome first. This driver does not authorize resume or abort.
/// </remarks>
public sealed class NamespaceRemovalRecovery(
    INamespaceLifecycleStorage storage,
    Func<NamespaceLifecycle, Task> execute,
    TimeProvider clock)
{
    bool _running;

    /// <summary>Attempts bounded progress, preserving consumed budgets across activations.</summary>
    /// <param name="name">The canonical namespace name.</param>
    /// <returns>The latest persisted lifecycle state, if initialized.</returns>
    public async Task<NamespaceLifecycle?> Tick(EventStoreNamespaceName name)
    {
        if (_running) return await storage.Get(name);
        _running = true;
        try
        {
            var current = await storage.Get(name);
            if (current?.Status != NamespaceLifecycleStatus.Closing) return current;
            current = await RecoverInterruptedAttempt(current);
            if (current is null) return await storage.Get(name);
            if (current.Status != NamespaceLifecycleStatus.Closing || current.RetryAt > clock.GetUtcNow()) return current;
            current = await Write(current, current.BeginAttempt(clock.GetUtcNow()));
            if (current?.Status == NamespaceLifecycleStatus.Closing) await RunAttempt(current);
            return await storage.Get(name);
        }
        finally
        {
            _running = false;
        }
    }

    async Task<NamespaceLifecycle?> RecoverInterruptedAttempt(NamespaceLifecycle current) => current.AttemptInProgress
        ? await Write(current, current.RecoverOutstandingAttempt(clock.GetUtcNow()))
        : current;

    async Task RunAttempt(NamespaceLifecycle current)
    {
        while (current.Status == NamespaceLifecycleStatus.Closing)
        {
            var issued = await PrepareStage(current);
            if (issued is null) return;
            current = issued;
            if (!await ExecuteStage(current)) return;
            var completed = await Write(current, current.CompleteStage(clock.GetUtcNow()));
            if (completed is null) return;
            current = completed;
        }
    }

    async Task<NamespaceLifecycle?> PrepareStage(NamespaceLifecycle current)
    {
        if (current.Deadline is null || current.Deadline <= clock.GetUtcNow())
        {
            await Write(current, current.RequireRecovery("NamespaceRemovalDeadlineExceeded"));
            return null;
        }
        return current.StageAttempts == 0 ? await Write(current, current.IssueStage()) : current;
    }

    async Task<bool> ExecuteStage(NamespaceLifecycle current)
    {
        var remaining = (current.Deadline ?? clock.GetUtcNow()) - clock.GetUtcNow();
        if (remaining <= TimeSpan.Zero)
        {
            await Write(current, current.RequireRecovery("NamespaceRemovalDeadlineExceeded"));
            return false;
        }
        try
        {
            var error = await ObserveEffect(current).WaitAsync(remaining, clock);
            if (error is null) return true;
            var failed = current.DestructiveAttemptOutstanding
                ? current.RequireRecovery("NamespaceRemovalOutcomeUnknown")
                : current.FailAttempt("NamespaceRemovalFailed", clock.GetUtcNow());
            await Write(current, failed);
        }
        catch (TimeoutException)
        {
            // WaitAsync never cancels the provider task. Preserve intent; a new tick must not overlap it.
            await Write(current, current.RequireRecovery("NamespaceRemovalOutcomeUnknown"));
        }
        return false;
    }

    async Task<Exception?> ObserveEffect(NamespaceLifecycle current)
    {
        try
        {
            await execute(current);
            return null;
        }
        catch (Exception error)
        {
            // Also observe failures arriving after the observation deadline, without advancing a stale checkpoint.
            return error;
        }
    }

    async Task<NamespaceLifecycle?> Write(NamespaceLifecycle previous, NamespaceLifecycle next) =>
        await storage.TryWrite(previous.Namespace, previous.Revision, next) ? next : null;
}
