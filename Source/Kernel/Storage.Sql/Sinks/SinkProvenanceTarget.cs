// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Binds an admitted physical database to the actual contexts used for preparation and payload work.
/// Credentials and payload search paths are deliberately not authorization identities.
/// </summary>
/// <param name="Provider">The connection provider.</param>
/// <param name="Server">The server or canonical SQLite path.</param>
/// <param name="Database">The database within the server.</param>
public sealed record SinkProvenanceTarget(string Provider, string Server, string Database)
{
    /// <summary>
    /// Captures the actual configured connection without opening it or resolving placement again.
    /// </summary>
    /// <param name="context">The actual context.</param>
    /// <returns>The non-secret placement identity.</returns>
    public static SinkProvenanceTarget For(ReadModelDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var server = connection.DataSource;
        if (connection is SqliteConnection sqlite) server = SqliteServer(sqlite);
        else if (string.IsNullOrEmpty(connection.Database)) throw new InvalidOperationException("Read-model storage is not available.");
        return new(connection.GetType().FullName ?? string.Empty, server, connection.Database);
    }

    static string SqliteServer(SqliteConnection connection)
    {
        var builder = new SqliteConnectionStringBuilder(connection.ConnectionString);
        if (builder.Mode != SqliteOpenMode.Memory && builder.DataSource != ":memory:") return Path.GetFullPath(builder.DataSource);
        return builder.Cache == SqliteCacheMode.Shared
            ? $"memory:{builder.DataSource}"
            : $"memory:{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(connection)}";
    }

    /// <summary>
    /// Refuses a changed physical target before any context preparation or payload work.
    /// </summary>
    /// <param name="context">The actual runtime or schema-preparation context.</param>
    public void Ensure(ReadModelDbContext context)
    {
        if (this != For(context)) throw new InvalidOperationException("Read-model storage is not available.");
    }
}
