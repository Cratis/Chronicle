// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Names the primary key constraint of a read model table, <c language="csharp">PK_{table}</c>, within the
/// identifier limits of the database.
/// </summary>
/// <remarks>
/// PostgreSQL and SQL Server scope constraint names to the schema, so two tables must never end up with the
/// same primary key name. Cutting a name that is too long to the limit would do exactly that: timestamped
/// backups of one read model differ only in their last characters. A name that does not fit therefore keeps
/// a readable prefix and ends in a hash of the whole table name.
/// </remarks>
internal static class PrimaryKeyNames
{
    /// <summary>
    /// PostgreSQL silently truncates identifiers to this many bytes (NAMEDATALEN - 1).
    /// </summary>
    internal const int PostgreSqlMaxIdentifierBytes = 63;

    /// <summary>
    /// SQL Server rejects identifiers longer than this many characters.
    /// </summary>
    internal const int SqlServerMaxIdentifierLength = 128;

    const int HashLength = 16;

    /// <summary>
    /// Gets the primary key constraint name for a table.
    /// </summary>
    /// <param name="databaseType">The <see cref="DatabaseType"/> the table lives in.</param>
    /// <param name="table">The name of the table.</param>
    /// <returns>The primary key constraint name.</returns>
    public static string For(DatabaseType databaseType, string table)
    {
        // Derived from the name the database actually stores, so a name read back from the catalog maps to
        // the same constraint name as the one the table was created with.
        var storedTable = TableIdentifier(databaseType, table);
        var name = $"PK_{storedTable}";
        if (Fits(databaseType, name))
        {
            return name;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(storedTable)))[..HashLength];
        var suffix = $"_{hash}";
        return $"{Truncate(databaseType, name, Limit(databaseType) - suffix.Length)}{suffix}";
    }

    /// <summary>
    /// Gets the name a database stores for a table name, which on PostgreSQL is the name truncated to its
    /// identifier limit.
    /// </summary>
    /// <param name="databaseType">The <see cref="DatabaseType"/> the table lives in.</param>
    /// <param name="table">The name of the table.</param>
    /// <returns>The stored table name.</returns>
    public static string TableIdentifier(DatabaseType databaseType, string table) =>
        databaseType == DatabaseType.PostgreSql ? Truncate(databaseType, table, PostgreSqlMaxIdentifierBytes) : table;

    static int Limit(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.PostgreSql => PostgreSqlMaxIdentifierBytes,
        DatabaseType.SqlServer => SqlServerMaxIdentifierLength,
        _ => int.MaxValue
    };

    static int LengthOf(DatabaseType databaseType, ReadOnlySpan<char> name) =>
        databaseType == DatabaseType.PostgreSql ? Encoding.UTF8.GetByteCount(name) : name.Length;

    static bool Fits(DatabaseType databaseType, string name) => LengthOf(databaseType, name) <= Limit(databaseType);

    static string Truncate(DatabaseType databaseType, string name, int limit)
    {
        var length = name.Length;
        while (length > 0 && (LengthOf(databaseType, name.AsSpan(0, length)) > limit || char.IsHighSurrogate(name[length - 1])))
        {
            length--;
        }

        return name[..length];
    }
}
