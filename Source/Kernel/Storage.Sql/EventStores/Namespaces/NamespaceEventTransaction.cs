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
/// <param name="retries">The shared cancellable attempt budget.</param>
internal sealed class NamespaceEventTransaction<TContext>(Func<Task<DbContextScope<TContext>>> open, NamespaceCommitRetries retries)
    where TContext : DbContext
{
    /// <summary>Initializes a protected transaction with bounded, cancellable production backoff.</summary>
    /// <param name="open">Opens an already-provisioned namespace context.</param>
    public NamespaceEventTransaction(Func<Task<DbContextScope<TContext>>> open) : this(open, new NamespaceCommitRetries())
    {
    }

    /// <summary>Gets whether another transaction, including collision recovery, may start.</summary>
    public bool HasRemainingAttempts => retries.HasRemainingAttempts;

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
        using var lifetime = retries.Bind(context);
        lifetime.ThrowIfCancellationRequested();
        if (context.Incarnation.Value == Guid.Empty) throw new NamespaceCommitRejected();
        while (true)
        {
            await retries.BeginAttempt(lifetime);
            try
            {
                return await Attempt(lifetime, write);
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

    static async Task<T> CommitWork<T>(NamespaceCommitCancellation lifetime, TContext database, Func<TContext, CancellationToken, Task<T>> write)
    {
        var context = lifetime.Context;
        var cancellationToken = lifetime.Token;
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        lifetime.ThrowIfCancellationRequested();
        var rejection = await NamespaceMarkerStorage.GetFenceRejection(database, context.Incarnation, cancellationToken);
        if (rejection is not null) throw rejection;
        if (context.ActivationIncarnation != context.Incarnation) throw new NamespaceIncarnationRequiresReactivation();
        lifetime.ThrowIfCancellationRequested();
        var result = await write(database, cancellationToken);
        lifetime.ThrowIfCancellationRequested();
        await Commit(transaction, cancellationToken);
        return result;
    }

    async Task<T> Attempt<T>(NamespaceCommitCancellation lifetime, Func<TContext, CancellationToken, Task<T>> write)
    {
        await using var scope = await open();
        lifetime.ThrowIfCancellationRequested();
        return await ExecuteOnce(scope.DbContext, () => CommitWork(lifetime, scope.DbContext, write));
    }
}
