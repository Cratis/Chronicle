// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Coordinates schema changes against a physical database across database instances in this process.
/// </summary>
internal sealed class DatabaseMigrationLock
{
    static readonly ConcurrentDictionary<string, DatabaseMigrationLock> _locks = new(StringComparer.Ordinal);
    long _generation;

    /// <summary>
    /// Gets the semaphore shared by migrations and resets of this target.
    /// </summary>
    internal SemaphoreSlim Semaphore { get; } = new(1, 1);

    /// <summary>
    /// Gets or sets the reset generation, advanced only while holding <see cref="Semaphore"/>.
    /// </summary>
    internal long Generation
    {
        get => Volatile.Read(ref _generation);
        set => Volatile.Write(ref _generation, value);
    }

    /// <summary>
    /// Gets the lock for the physical target, independent of SQLite connection-string spelling.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The shared lock.</returns>
    internal static DatabaseMigrationLock For(string connectionString)
    {
        var key = connectionString;
        if (connectionString.GetDatabaseType() == DatabaseType.Sqlite)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            var source = builder.DataSource;
            if (source.Length > 0 && source != ":memory:" && builder.Mode != SqliteOpenMode.Memory)
            {
                if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.IsFile)
                {
                    source = uri.LocalPath;
                }

                key = $"sqlite:{Path.GetFullPath(source)}";
            }
        }

        return _locks.GetOrAdd(key, static _ => new());
    }
}
