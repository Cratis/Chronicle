// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Commits a namespace conflict write and its event/receipt work on one context and connection.
/// Each definite-conflict retry opens a fresh scope, preserving the supplied lifetime and attempt budget.
/// </summary>
/// <typeparam name="TContext">The namespace-local event or receipt context.</typeparam>
/// <param name="open">Opens an already-provisioned destination; no migration may run in the transaction.</param>
internal sealed class NamespaceEventTransaction<TContext>(Func<Task<DbContextScope<TContext>>> open)
    where TContext : DbContext
{
    const int MaximumAttempts = 3;
    int _attempts;

    /// <summary>Gets whether another transaction, including collision recovery, may start.</summary>
    public bool HasRemainingAttempts => _attempts < MaximumAttempts;

    /// <summary>Runs work only after the exact marker write and activation comparison.</summary>
    /// <typeparam name="T">The value returned only after a definite commit.</typeparam>
    /// <param name="context">The immutable expected lifetime and activation state.</param>
    /// <param name="write">Work using the supplied context and cancellation, without opening another connection.</param>
    /// <returns>The committed result.</returns>
    /// <exception cref="NamespaceCommitRejected">The expected marker is absent, closed, or replaced.</exception>
    /// <exception cref="NamespaceIncarnationRequiresReactivation">The activation captured another or unknown lifetime.</exception>
    /// <exception cref="NamespaceCommitOutcomeUnknown">Commit was submitted without a definite outcome.</exception>
    /// <exception cref="InvalidOperationException">The transaction attempt budget is exhausted.</exception>
    public async Task<T> Execute<T>(NamespaceCommitContext context, Func<TContext, CancellationToken, Task<T>> write)
    {
        context.ActivationCancellation.ThrowIfCancellationRequested();
        if (context.Incarnation.Value == Guid.Empty) throw new NamespaceCommitRejected();
        if (!HasRemainingAttempts) throw new InvalidOperationException("The namespace transaction attempt budget is exhausted.");
        for (_attempts++; ; _attempts++)
        {
            context.ActivationCancellation.ThrowIfCancellationRequested();
            try
            {
                return await Attempt(context, write);
            }
            catch (Exception error) when (StorageWriteConflicts.IsTransactionConflict(error) && HasRemainingAttempts)
            {
                // Only a definite provider conflict permits a fresh transaction body, never an unknown commit.
            }
        }
    }

    static async Task Commit(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception error) when (!StorageWriteConflicts.IsTransactionConflict(error))
        {
            // Cancellation after dispatch is also unknown; never inspect a sessionless receipt to settle it.
            throw new NamespaceCommitOutcomeUnknown(error);
        }
    }

    static async Task<T> ExecuteOnce<T>(DbContext database, Func<Task<T>> operation)
    {
        // SQL connection factories enable provider retries. Enter their required execution scope, but
        // return failures as data so only our definite-conflict budget can replay a transaction body.
        var outcome = await database.Database.CreateExecutionStrategy().ExecuteAsync<(T Result, ExceptionDispatchInfo? Failure)>(async () =>
        {
            try
            {
                return (Result: await operation(), Failure: null);
            }
            catch (Exception error)
            {
                return (Result: default(T)!, Failure: ExceptionDispatchInfo.Capture(error));
            }
        });
        outcome.Failure?.Throw();
        return outcome.Result;
    }

    static async Task<T> CommitWork<T>(NamespaceCommitContext context, TContext database, Func<TContext, CancellationToken, Task<T>> write)
    {
        var cancellationToken = context.ActivationCancellation;
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var rejection = await NamespaceMarkerStorage.GetFenceRejection(database, context.Incarnation, cancellationToken);
        if (rejection is not null) throw rejection;
        if (context.ActivationIncarnation != context.Incarnation) throw new NamespaceIncarnationRequiresReactivation();
        cancellationToken.ThrowIfCancellationRequested();
        var result = await write(database, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await Commit(transaction, cancellationToken);
        return result;
    }

    async Task<T> Attempt<T>(NamespaceCommitContext context, Func<TContext, CancellationToken, Task<T>> write)
    {
        await using var scope = await open();
        return await ExecuteOnce(scope.DbContext, () => CommitWork(context, scope.DbContext, write));
    }
}
