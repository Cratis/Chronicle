// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>A command that counts every execution at the ADO.NET level, whoever issued it (EF Core or direct code).</summary>
/// <param name="inner">The real command.</param>
/// <param name="connection">The wrapping connection that created it.</param>
/// <param name="counter">The counter to record into.</param>
internal sealed class CountingDbCommand(DbCommand inner, DbConnection connection, CommandCounter counter) : DbCommand
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The connection owns its commands, not the other way around.")]
    DbConnection? _connection = connection;
    DbTransaction? _transaction;

    /// <inheritdoc/>
    [AllowNull]
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "A counting wrapper forwards the command text it was given.")]
    public override string CommandText
    {
        get => inner.CommandText;
        set => inner.CommandText = value;
    }

    /// <inheritdoc/>
    public override int CommandTimeout
    {
        get => inner.CommandTimeout;
        set => inner.CommandTimeout = value;
    }

    /// <inheritdoc/>
    public override CommandType CommandType
    {
        get => inner.CommandType;
        set => inner.CommandType = value;
    }

    /// <inheritdoc/>
    public override bool DesignTimeVisible
    {
        get => inner.DesignTimeVisible;
        set => inner.DesignTimeVisible = value;
    }

    /// <inheritdoc/>
    public override UpdateRowSource UpdatedRowSource
    {
        get => inner.UpdatedRowSource;
        set => inner.UpdatedRowSource = value;
    }

    /// <inheritdoc/>
    protected override DbConnection? DbConnection
    {
        get => _connection;
        set
        {
            _connection = value;
            inner.Connection = value is CountingDbConnection counting ? counting.Inner : value;
        }
    }

    /// <inheritdoc/>
    protected override DbParameterCollection DbParameterCollection => inner.Parameters;

    /// <inheritdoc/>
    protected override DbTransaction? DbTransaction
    {
        get => _transaction;
        set
        {
            _transaction = value;
            inner.Transaction = value is CountingDbTransaction counting ? counting.Inner : value;
        }
    }

    /// <inheritdoc/>
    public override void Cancel() => inner.Cancel();

    /// <inheritdoc/>
    public override int ExecuteNonQuery()
    {
        Count();
        return inner.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        Count();
        return inner.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override object? ExecuteScalar()
    {
        Count();
        return inner.ExecuteScalar();
    }

    /// <inheritdoc/>
    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        Count();
        return inner.ExecuteScalarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override void Prepare() => inner.Prepare();

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override DbParameter CreateDbParameter() => inner.CreateParameter();

    /// <inheritdoc/>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        Count();
        return inner.ExecuteReader(behavior);
    }

    /// <inheritdoc/>
    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        Count();
        return inner.ExecuteReaderAsync(behavior, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
        base.Dispose(disposing);
    }

    void Count() => counter.Record(SqlVerb.Of(inner.CommandText));
}
