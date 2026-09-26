// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Derives the physical database name for an event store, a namespace, or a set of read models.
/// </summary>
/// <remarks>
/// The names are built by concatenation - a cluster database name, then the event store, then the namespace -
/// and a database server has a hard limit on an identifier. PostgreSQL truncates silently at 63 bytes, so two
/// logically distinct scopes whose names agreed up to that point resolved to one physical database and shared
/// an event log with no error anywhere: a cross-tenant leak that looks exactly like working software (#4137).
/// <para>
/// Rather than truncate blindly, a name over budget keeps as much of itself as fits and ends with a short hash
/// of the whole logical name. The prefix keeps it recognizable to an operator looking for it by hand, and the
/// hash is what makes it unique. SHA-256 is used rather than string hashing because <see cref="string.GetHashCode()"/>
/// is randomized per process, so the same logical name would resolve to a different database after a restart.
/// </para>
/// </remarks>
public static class DerivedDatabaseName
{
    /// <summary>
    /// The maximum identifier length in bytes for PostgreSQL - NAMEDATALEN of 64 less the terminator.
    /// </summary>
    public const int PostgreSqlMaxBytes = 63;

    /// <summary>
    /// The maximum identifier length in bytes for SQL Server.
    /// </summary>
    public const int SqlServerMaxBytes = 128;

    /// <summary>
    /// The number of hash characters appended to a name that does not fit.
    /// </summary>
    public const int HashLength = 8;

    /// <summary>
    /// Get the maximum identifier length in bytes for a <see cref="DatabaseType"/>.
    /// </summary>
    /// <param name="databaseType">The <see cref="DatabaseType"/> to get for.</param>
    /// <returns>The budget in bytes, or <see langword="null"/> when the type imposes no identifier limit worth enforcing.</returns>
    public static int? MaxBytesFor(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.PostgreSql => PostgreSqlMaxBytes,
        DatabaseType.SqlServer => SqlServerMaxBytes,
        _ => null
    };

    /// <summary>
    /// Derive a database name that fits within a byte budget without losing uniqueness.
    /// </summary>
    /// <param name="name">The full logical name.</param>
    /// <param name="maxBytes">The budget in bytes, or <see langword="null"/> to impose none.</param>
    /// <returns>The name itself when it fits, otherwise a truncated prefix followed by a hash of the whole name.</returns>
    /// <remarks>
    /// The budget is measured in UTF-8 bytes rather than characters: a namespace named in a script outside ASCII
    /// costs more than its length suggests, and the server counts bytes.
    /// </remarks>
    public static string WithinBudget(string name, int? maxBytes)
    {
        if (maxBytes is not { } budget || Encoding.UTF8.GetByteCount(name) <= budget)
        {
            return name;
        }

        var hash = HashOf(name);
        var prefix = TruncateToBytes(name, budget - HashLength - 1);
        return $"{prefix}+{hash}";
    }

    /// <summary>
    /// Get the stable short hash used to keep a truncated name unique.
    /// </summary>
    /// <param name="name">The full logical name to hash.</param>
    /// <returns>The first characters of the lowercase hexadecimal SHA-256 of the name.</returns>
    static string HashOf(string name) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..HashLength];

    /// <summary>
    /// Truncate a string to a number of UTF-8 bytes without splitting a character.
    /// </summary>
    /// <param name="value">The value to truncate.</param>
    /// <param name="maxBytes">The maximum number of bytes.</param>
    /// <returns>The truncated value.</returns>
    static string TruncateToBytes(string value, int maxBytes)
    {
        if (maxBytes <= 0)
        {
            return string.Empty;
        }

        if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
        {
            return value;
        }

        var bytes = 0;
        var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(value);
        var length = 0;
        while (enumerator.MoveNext())
        {
            var element = (string)enumerator.Current;
            var elementBytes = Encoding.UTF8.GetByteCount(element);
            if (bytes + elementBytes > maxBytes)
            {
                break;
            }

            bytes += elementBytes;
            length += element.Length;
        }

        return value[..length];
    }
}
