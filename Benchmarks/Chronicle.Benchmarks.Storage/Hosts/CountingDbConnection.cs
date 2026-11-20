// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Transaction = System.Transactions.Transaction;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// A connection that wraps the provider's own, so that every command and transaction control that reaches the
/// driver is counted: commands EF Core issues and commands issued directly on the connection alike.
/// </summary>
/// <param name="inner">The real connection.</param>
/// <param name="counter">The counter to record into.</param>
internal sealed class CountingDbConnection(DbConnection inner, CommandCounter counter) : DbConnection
{
    bool _subscribed;

    /// <inheritdoc/>
    public override event StateChangeEventHandler? StateChange
    {
        add
        {
            Subscribe();
            base.StateChange += value;
        }
        remove => base.StateChange -= value;
    }

    /// <summary>Gets the real connection.</summary>
    public DbConnection Inner => inner;

    /// <inheritdoc/>
    [AllowNull]
    public override string ConnectionString
    {
        get => inner.ConnectionString;
        set => inner.ConnectionString = value;
    }

    /// <inheritdoc/>
    public override string Database => inner.Database;

    /// <inheritdoc/>
    public override string DataSource => inner.DataSource;

    /// <inheritdoc/>
    public override string ServerVersion => inner.ServerVersion;

    /// <inheritdoc/>
    public override ConnectionState State => inner.State;

    /// <inheritdoc/>
    public override int ConnectionTimeout => inner.ConnectionTimeout;

    /// <inheritdoc/>
    public override void ChangeDatabase(string databaseName) => inner.ChangeDatabase(databaseName);

    /// <inheritdoc/>
    public override void Close() => inner.Close();

    /// <inheritdoc/>
    public override Task CloseAsync() => inner.CloseAsync();

    /// <inheritdoc/>
    public override void Open() => inner.Open();

    /// <inheritdoc/>
    public override Task OpenAsync(CancellationToken cancellationToken) => inner.OpenAsync(cancellationToken);

    /// <inheritdoc/>
    public override void EnlistTransaction(Transaction? transaction) => inner.EnlistTransaction(transaction);

    /// <inheritdoc/>
    public override DataTable GetSchema() => inner.GetSchema();

    /// <inheritdoc/>
    public override DataTable GetSchema(string collectionName) => inner.GetSchema(collectionName);

    /// <inheritdoc/>
    public override DataTable GetSchema(string collectionName, string?[] restrictionValues) => inner.GetSchema(collectionName, restrictionValues);

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        new CountingDbTransaction(inner.BeginTransaction(isolationLevel), this, counter);

    /// <inheritdoc/>
    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) =>
        new CountingDbTransaction(await inner.BeginTransactionAsync(isolationLevel, cancellationToken), this, counter);

    /// <inheritdoc/>
    protected override DbCommand CreateDbCommand() => new CountingDbCommand(inner.CreateCommand(), this, counter);

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
        base.Dispose(disposing);
    }

    void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true;
        inner.StateChange += (_, change) => OnStateChange(change);
    }
}
