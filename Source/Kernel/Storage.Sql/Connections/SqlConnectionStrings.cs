// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Applies resolved placement, schema and pool settings to connection strings.
/// </summary>
internal static class SqlConnectionStrings
{
    /// <summary>
    /// Check whether a resolution decides where a database is, rather than leaving it to Chronicle's naming.
    /// </summary>
    /// <param name="resolved">The <see cref="ResolvedConnection"/>.</param>
    /// <returns>True when a connection string or database was resolved.</returns>
    internal static bool OwnsPlacement(ResolvedConnection resolved) =>
        !string.IsNullOrEmpty(resolved.ConnectionString) || !string.IsNullOrEmpty(resolved.Database);

    /// <summary>
    /// Apply a resolved connection string and database over the configured connection string.
    /// </summary>
    /// <param name="configured">The configured connection string.</param>
    /// <param name="resolved">The <see cref="ResolvedConnection"/>.</param>
    /// <returns>The connection string that places the database.</returns>
    internal static string WithPlacement(string configured, ResolvedConnection resolved)
    {
        var connectionString = string.IsNullOrEmpty(resolved.ConnectionString) ? configured : resolved.ConnectionString;
        if (string.IsNullOrEmpty(resolved.Database))
        {
            return connectionString;
        }

        return connectionString.GetDatabaseType() switch
        {
            DatabaseType.SqlServer => new SqlConnectionStringBuilder(connectionString) { InitialCatalog = resolved.Database }.ConnectionString,
            DatabaseType.PostgreSql => new NpgsqlConnectionStringBuilder(connectionString) { Database = resolved.Database }.ConnectionString,
            _ => new SqliteConnectionStringBuilder(connectionString) { DataSource = resolved.Database }.ConnectionString
        };
    }

    /// <summary>
    /// Apply connection pool settings to a PostgreSQL connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="pooling">The PostgreSQL connection pool settings.</param>
    /// <returns>The connection string with pool settings, unchanged for other providers.</returns>
    /// <remarks>
    /// Connection string values take precedence over the configured pool settings, including synonyms.
    /// The maximum pool size is at least the minimum pool size and at least one, the idle lifetime is at least one
    /// second, and the pruning interval is limited to the idle lifetime while remaining at least one second.
    /// </remarks>
    internal static string WithPooling(string connectionString, PostgreSqlPooling pooling)
    {
        if (connectionString.GetDatabaseType() != DatabaseType.PostgreSql)
        {
            return connectionString;
        }

        // Npgsql resolves repeated keys and aliases in the order they appear, so the string is parsed by Npgsql
        // itself, and a configured value only fills a setting the string does not state.
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var stated = new DbConnectionStringBuilder { ConnectionString = connectionString };
        if (!States(stated, "Maximum Pool Size", nameof(NpgsqlConnectionStringBuilder.MaxPoolSize)))
        {
            builder.MaxPoolSize = Math.Max(1, pooling.MaxPoolSize);
        }

        if (!States(stated, "Connection Idle Lifetime", nameof(NpgsqlConnectionStringBuilder.ConnectionIdleLifetime)))
        {
            builder.ConnectionIdleLifetime = Math.Max(1, pooling.ConnectionIdleLifetimeSeconds);
        }

        if (!States(stated, "Connection Pruning Interval", nameof(NpgsqlConnectionStringBuilder.ConnectionPruningInterval)))
        {
            builder.ConnectionPruningInterval = Math.Max(1, pooling.ConnectionPruningIntervalSeconds);
        }

        builder.MaxPoolSize = Math.Max(Math.Max(builder.MaxPoolSize, builder.MinPoolSize), 1);
        builder.ConnectionIdleLifetime = Math.Max(1, builder.ConnectionIdleLifetime);
        builder.ConnectionPruningInterval = Math.Max(1, Math.Min(builder.ConnectionPruningInterval, builder.ConnectionIdleLifetime));
        return builder.ConnectionString;
    }

    /// <summary>
    /// Apply a schema to a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="schema">The schema. Empty leaves the connection string as it is.</param>
    /// <returns>The connection string, connecting into the schema where the provider allows it.</returns>
    /// <remarks>
    /// PostgreSQL gets the schema first on its search path, so every unqualified table — EF Core's included — lives in it.
    /// SQL Server has no per-connection schema: it uses the default schema of the database user, which Chronicle
    /// verifies instead.
    /// </remarks>
    internal static string WithSchema(string connectionString, string schema) =>
        string.IsNullOrEmpty(schema) || connectionString.GetDatabaseType() != DatabaseType.PostgreSql
            ? connectionString
            : new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema }.ConnectionString;

    static bool States(DbConnectionStringBuilder stated, params string[] keywords) =>
        keywords.Any(stated.ContainsKey);
}
