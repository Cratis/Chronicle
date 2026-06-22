// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// An EF Core connection interceptor that applies SQLite pragmas needed for concurrent access on
/// every opened connection.
/// </summary>
/// <remarks>
/// The Chronicle kernel writes to a single SQLite database from many Orleans grains concurrently
/// (event-sequence appends plus read-model/observer-state sink writes). SQLite's default
/// rollback-journal mode takes a database-wide exclusive lock for every write, so under that load
/// writers collide and fail immediately with <c>SQLITE_BUSY</c>, stalling observer catch-up. WAL
/// mode lets readers proceed during a write and lets writers append without locking out readers, and
/// <c>busy_timeout</c> makes a contending writer wait for the lock instead of failing instantly.
/// Durability is preserved: <c>synchronous</c> is intentionally left at its default so a committed
/// transaction is still flushed.
/// </remarks>
public sealed class SqlitePragmaConnectionInterceptor : DbConnectionInterceptor
{
    /// <summary>
    /// A shared, stateless instance of the interceptor.
    /// </summary>
    public static readonly SqlitePragmaConnectionInterceptor Instance = new();

    /// <summary>
    /// The pragmas applied on every open. WAL persists in the database header (set once, sticks);
    /// <c>busy_timeout</c> is per-connection and is re-applied each time. 30s is comfortably above
    /// any genuine catch-up window.
    /// </summary>
    const string Pragmas = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=30000;";

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
