// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Infers the storage type from the shape of a storage connection string.
/// </summary>
/// <remarks>
/// The SQL rules mirror the connection string rules used when the SQL storage opens its database, so the
/// inferred type matches the provider that would be selected for the same connection details.
/// </remarks>
public static class StorageTypeInference
{
    static readonly string[] _sqlServerKeywords =
    [
        "initial catalog=",
        "database=",
        "user id=",
        "integrated security=",
        "trust server certificate=",
        "trustservercertificate=",
        "encrypt=",
        "multipleactiveresultsets=",
        "application name="
    ];

    /// <summary>
    /// Infers the storage type from the connection details.
    /// </summary>
    /// <param name="connectionDetails">The storage connection details.</param>
    /// <returns>The <see cref="StorageType"/> value, or null when the connection details are not recognizable.</returns>
    public static string? Infer(string connectionDetails)
    {
        if (string.IsNullOrWhiteSpace(connectionDetails))
        {
            return null;
        }

        if (IsMongoDB(connectionDetails)) return StorageType.MongoDB;
        if (IsMsSql(connectionDetails)) return StorageType.MsSql;
        if (IsPostgreSql(connectionDetails)) return StorageType.PostgreSql;
        if (IsSqlite(connectionDetails)) return StorageType.Sqlite;
        return null;
    }

    static bool IsMongoDB(string value) =>
        value.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase);

    static bool IsMsSql(string value) =>
        (Contains(value, "server=") || Contains(value, "data source=")) && _sqlServerKeywords.Any(_ => Contains(value, _));

    static bool IsPostgreSql(string value) => Contains(value, "host=") && Contains(value, "database=");

    static bool IsSqlite(string value) => Contains(value, "data source=") || Contains(value, "filename=");

    static bool Contains(string value, string keyword) => value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
