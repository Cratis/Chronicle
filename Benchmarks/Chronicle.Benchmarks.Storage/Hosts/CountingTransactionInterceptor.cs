// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// Counts transaction commits and rollbacks (and savepoint operations) EF Core issues, so SQL counts line up with
/// MongoDB's `commitTransaction` and `abortTransaction`. `BEGIN` is not counted. It observes the transaction
/// and never replaces the connection.
/// </summary>
/// <param name="counter">The counter to record into.</param>
internal sealed class CountingTransactionInterceptor(CommandCounter counter) : DbTransactionInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result) => Count(SqlVerb.Commit, result);

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Count(SqlVerb.Commit, result));

    /// <inheritdoc/>
    public override InterceptionResult TransactionRollingBack(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result) => Count(SqlVerb.Rollback, result);

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult> TransactionRollingBackAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Count(SqlVerb.Rollback, result));

    /// <inheritdoc/>
    public override InterceptionResult CreatingSavepoint(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result) => Count(SqlVerb.Savepoint, result);

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult> CreatingSavepointAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Count(SqlVerb.Savepoint, result));

    /// <inheritdoc/>
    public override InterceptionResult RollingBackToSavepoint(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result) => Count(SqlVerb.Savepoint, result);

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult> RollingBackToSavepointAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Count(SqlVerb.Savepoint, result));

    InterceptionResult Count(string verb, InterceptionResult result)
    {
        counter.Record(verb);
        return result;
    }
}
