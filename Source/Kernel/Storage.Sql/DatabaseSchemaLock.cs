// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Serializes schema changes to a database across every kernel process that uses it.
/// </summary>
/// <remarks>
/// <para>
/// The in-process semaphores only keep grains of one kernel apart. Several kernels starting together, or the old and
/// new nodes of a rolling deploy, would otherwise race to create the same tables, columns and indexes. Schema changes
/// therefore run while holding a lock in the database itself — an application lock on SQL Server, an advisory lock on
/// PostgreSQL. SQLite is a local file reached by one process, so it relies on the in-process semaphores alone.
/// </para>
/// <para>
/// The lock is held on a dedicated connection to the same database, connected the same way as the context — with the
/// identity resolved for it — so it works with access tokens as well as passwords. That connection is not pooled:
/// closing it ends the session and with it any lock it still holds.
/// </para>
/// <para>
/// The lock is reentrant within one asynchronous flow, so a schema change that ensures another table of the same
/// database on the way does not wait for itself.
/// </para>
/// </remarks>
internal static class DatabaseSchemaLock
{
    /// <summary>
    /// How long to wait for another kernel to finish changing the schema.
    /// </summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    static readonly AsyncLocal<ImmutableHashSet<string>?> _held = new();

    /// <summary>
    /// Get the lock for the provider a connection belongs to.
    /// </summary>
    /// <param name="connection">The <see cref="DbConnection"/>.</param>
    /// <returns>The <see cref="IDatabaseSchemaLock"/>, or null when the provider needs none.</returns>
    internal static IDatabaseSchemaLock? For(DbConnection connection) => connection switch
    {
        SqlConnection => SqlServerDatabaseSchemaLock.Instance,
        NpgsqlConnection => PostgreSqlDatabaseSchemaLock.Instance,
        _ => null
    };

    /// <summary>
    /// Get the name of the lock for a database.
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <returns>The resource name.</returns>
    internal static string ResourceFor(string database) => $"Cratis.Chronicle.Schema:{database}";

    /// <summary>
    /// Run a schema change while holding the schema lock of the database a context connects to.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> whose database changes.</param>
    /// <param name="change">The schema change.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DatabaseSchemaLockNotAcquired">Another kernel held the lock for longer than <see cref="DefaultTimeout"/>.</exception>
    internal static Task Run(DbContext context, Func<Task> change) => Run(context, change, DefaultTimeout);

    /// <summary>
    /// Run a schema change while holding the schema lock of the database a context connects to.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> whose database changes.</param>
    /// <param name="change">The schema change.</param>
    /// <param name="timeout">How long to wait for the lock.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DatabaseSchemaLockNotAcquired">Another kernel held the lock for longer than the timeout.</exception>
    internal static async Task Run(DbContext context, Func<Task> change, TimeSpan timeout)
    {
        var contextConnection = context.Database.GetDbConnection();
        var schemaLock = For(contextConnection);
        var key = $"{contextConnection.DataSource}|{contextConnection.Database}";
        var held = _held.Value ?? [];
        if (schemaLock is null || held.Contains(key))
        {
            await change();
            return;
        }

        var resource = ResourceFor(contextConnection.Database);
        await using var connection = await OpenDedicatedConnection(context, contextConnection);
        await schemaLock.Acquire(connection, resource, timeout);
        _held.Value = held.Add(key);
        try
        {
            await change();
        }
        finally
        {
            _held.Value = held;
            await Release(schemaLock, connection, resource);
        }
    }

    static async Task Release(IDatabaseSchemaLock schemaLock, DbConnection connection, string resource)
    {
        try
        {
            await schemaLock.Release(connection, resource);
        }
        catch (DbException) when (connection.State != ConnectionState.Open)
        {
            // A broken connection has ended its session, and the lock with it.
        }
    }

    static async Task<DbConnection> OpenDedicatedConnection(DbContext context, DbConnection contextConnection)
    {
        var options = context.GetService<IDbContextOptions>();

        // The configured connection string, not the open connection's, which may no longer carry the password.
        var configured = RelationalOptionsExtension.Extract(options).ConnectionString ?? contextConnection.ConnectionString;
        var factory = DbProviderFactories.GetFactory(contextConnection)!;
        var builder = factory.CreateConnectionStringBuilder()!;
        builder.ConnectionString = configured;
        builder["Pooling"] = false;

        var connection = factory.CreateConnection()!;
        try
        {
            connection.ConnectionString = builder.ConnectionString;
            var identity = options.FindExtension<CoreOptionsExtension>()?.Interceptors?.OfType<ResolvedIdentityInterceptor>().FirstOrDefault();
            if (identity is not null)
            {
                await identity.Prepare(connection);
            }

            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
