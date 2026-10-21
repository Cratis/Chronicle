// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Names indexes within provider identifier limits, including PostgreSQL's byte limit.
/// </summary>
internal static class ReadModelIndexNames
{
    /// <summary>
    /// The prefix of every index Chronicle creates for a read model.
    /// </summary>
    internal const string Prefix = "chronicle_idx_";

    /// <summary>
    /// Gets the index name for a column within the provider's identifier limits.
    /// </summary>
    /// <param name="databaseType">The database provider.</param>
    /// <param name="table">The table name.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The index name.</returns>
    internal static string For(DatabaseType databaseType, string table, string column)
    {
        table = PrimaryKeyNames.TableIdentifier(databaseType, table);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{table.Length}:{table}{column}")))[..16];
        var name = $"{Prefix}{table}_{column}";
        var limit = databaseType switch
        {
            DatabaseType.PostgreSql => PrimaryKeyNames.PostgreSqlMaxIdentifierBytes,
            DatabaseType.SqlServer => PrimaryKeyNames.SqlServerMaxIdentifierLength,
            _ => int.MaxValue
        };
        var length = name.Length;
        while (length > 0 && (LengthOf(name.AsSpan(0, length), databaseType) > limit - 17 || char.IsHighSurrogate(name[length - 1])))
        {
            length--;
        }

        return $"{name[..length]}_{hash}";
    }

    static int LengthOf(ReadOnlySpan<char> name, DatabaseType databaseType) =>
        databaseType == DatabaseType.PostgreSql ? Encoding.UTF8.GetByteCount(name) : name.Length;
}
