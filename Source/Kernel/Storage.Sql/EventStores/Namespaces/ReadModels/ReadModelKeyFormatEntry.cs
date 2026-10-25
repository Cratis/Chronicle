// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Records the key format of one physical read-model table. A table without a row is <see cref="ReadModelKeyFormat.Legacy"/>.
/// </summary>
/// <param name="Id">The fixed-size lookup key for the full table name.</param>
/// <param name="TableName">The complete physical table name.</param>
/// <param name="Format">The stored format, "building-k1" or "k1".</param>
internal record ReadModelKeyFormatEntry(string Id, string TableName, string Format)
{
    const string BuildingK1Value = "building-k1";
    const string K1Value = "k1";

    /// <summary>
    /// Gets an identifier without truncating the table name. The full name is also stored as a value.
    /// </summary>
    /// <param name="tableName">The physical table name.</param>
    /// <returns>The lookup key.</returns>
    public static string IdentifierFor(string tableName) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tableName)));

    /// <summary>
    /// Creates an entry for a table.
    /// </summary>
    /// <param name="tableName">The physical table name.</param>
    /// <param name="format">The format to record. <see cref="ReadModelKeyFormat.Legacy"/> has no row and is not valid here.</param>
    /// <returns>The entry.</returns>
    public static ReadModelKeyFormatEntry For(string tableName, ReadModelKeyFormat format) =>
        new(IdentifierFor(tableName), tableName, ToStored(format));

    /// <summary>
    /// Converts a stored value to a format.
    /// </summary>
    /// <param name="tableName">The physical table name, used for the failure message.</param>
    /// <param name="stored">The stored value, or null when the table has no row.</param>
    /// <returns>The format.</returns>
    /// <exception cref="UnknownReadModelKeyFormat">The stored value is not known.</exception>
    public static ReadModelKeyFormat FromStored(string tableName, string? stored) => stored switch
    {
        null => ReadModelKeyFormat.Legacy,
        BuildingK1Value => ReadModelKeyFormat.BuildingK1,
        K1Value => ReadModelKeyFormat.K1,
        _ => throw new UnknownReadModelKeyFormat(tableName, stored)
    };

    static string ToStored(ReadModelKeyFormat format) => format switch
    {
        ReadModelKeyFormat.BuildingK1 => BuildingK1Value,
        ReadModelKeyFormat.K1 => K1Value,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Only building-k1 and k1 are recorded.")
    };
}
