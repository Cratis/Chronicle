// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Where read model tables physically live: server, port, database and schema, never a credential.
/// </summary>
/// <param name="Provider">The SQL provider.</param>
/// <param name="Server">The server or data source.</param>
/// <param name="Port">The port, if given.</param>
/// <param name="Database">The database, or the file for a data source.</param>
/// <param name="Schema">The schema.</param>
internal readonly record struct ReadModelTablePlacement(DatabaseType Provider, string Server, string Port, string Database, string Schema)
{
    static readonly ConcurrentDictionary<(string ConnectionString, string Schema), ReadModelTablePlacement?> _parsed = new();

    /// <summary>
    /// Gets the placement a connection string points at, parsed once per distinct connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="schema">The resolved schema.</param>
    /// <returns>The placement, or null when the connection string cannot be understood.</returns>
    internal static ReadModelTablePlacement? Of(string connectionString, string schema) =>
        _parsed.GetOrAdd((connectionString, schema), static key => Parse(key.ConnectionString, key.Schema));

    /// <summary>
    /// Conservatively compares placements, including case variants that may share a database or schema.
    /// </summary>
    /// <param name="other">The other placement.</param>
    /// <returns>Whether the placements could refer to the same physical schema.</returns>
    internal bool Overlaps(ReadModelTablePlacement other) =>
        Provider == other.Provider && string.Equals(Server, other.Server, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Port, other.Port, StringComparison.OrdinalIgnoreCase) && string.Equals(Database, other.Database, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Schema, other.Schema, StringComparison.OrdinalIgnoreCase);

    static ReadModelTablePlacement? Parse(string connectionString, string schema)
    {
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            var provider = connectionString.GetDatabaseType();
            var server = Value(builder, "Host", "Server", "Data Source", "DataSource");
            var database = Value(builder, "Database", "Initial Catalog", "InitialCatalog");
            if (provider is not (DatabaseType.PostgreSql or DatabaseType.SqlServer or DatabaseType.Sqlite) || (server.Length == 0 && database.Length == 0))
            {
                return null;
            }

            return new(provider, server, Value(builder, "Port"), database, schema);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or InvalidOperationException or UnsupportedDatabaseType)
        {
            return null;
        }
    }

    static string Value(DbConnectionStringBuilder builder, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var value) && value is not null)
            {
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
            }
        }

        return string.Empty;
    }
}
