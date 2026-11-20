// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// A transaction that counts its commit and rollback, as MongoDB's commitTransaction and abortTransaction are counted.
/// Beginning a transaction is not counted: MongoDB starts a transaction on its first operation without a command of its own.
/// </summary>
/// <param name="inner">The real transaction.</param>
/// <param name="connection">The wrapping connection that began it.</param>
/// <param name="counter">The counter to record into.</param>
internal sealed class CountingDbTransaction(DbTransaction inner, DbConnection connection, CommandCounter counter) : DbTransaction
{
    /// <summary>Gets the real transaction.</summary>
    public DbTransaction Inner => inner;

    /// <inheritdoc/>
    public override IsolationLevel IsolationLevel => inner.IsolationLevel;

    /// <inheritdoc/>
    protected override DbConnection? DbConnection => connection;

    /// <inheritdoc/>
    public override void Commit()
    {
        counter.Record(SqlVerb.Commit);
        inner.Commit();
    }

    /// <inheritdoc/>
    public override Task CommitAsync(CancellationToken cancellationToken = default)
    {
        counter.Record(SqlVerb.Commit);
        return inner.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override void Rollback()
    {
        counter.Record(SqlVerb.Rollback);
        inner.Rollback();
    }

    /// <inheritdoc/>
    public override Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        counter.Record(SqlVerb.Rollback);
        return inner.RollbackAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override void Save(string savepointName)
    {
        counter.Record(SqlVerb.Savepoint);
        inner.Save(savepointName);
    }

    /// <inheritdoc/>
    public override Task SaveAsync(string savepointName, CancellationToken cancellationToken = default)
    {
        counter.Record(SqlVerb.Savepoint);
        return inner.SaveAsync(savepointName, cancellationToken);
    }

    /// <inheritdoc/>
    public override void Rollback(string savepointName)
    {
        counter.Record(SqlVerb.Savepoint);
        inner.Rollback(savepointName);
    }

    /// <inheritdoc/>
    public override Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default)
    {
        counter.Record(SqlVerb.Savepoint);
        return inner.RollbackAsync(savepointName, cancellationToken);
    }

    /// <inheritdoc/>
    public override void Release(string savepointName)
    {
        counter.Record(SqlVerb.Savepoint);
        inner.Release(savepointName);
    }

    /// <inheritdoc/>
    public override Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default)
    {
        counter.Record(SqlVerb.Savepoint);
        return inner.ReleaseAsync(savepointName, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
        base.Dispose(disposing);
    }
}
