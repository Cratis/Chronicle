// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Defines a system for managing migrations for unique constraint tables.
/// </summary>
public interface IUniqueConstraintMigrator
{
    /// <summary>
    /// Ensures the unique constraint table exists and has the correct schema.
    /// </summary>
    /// <param name="tableName">The name of the unique constraint table.</param>
    /// <param name="context">The <see cref="UniqueConstraintDbContext"/> for the table.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureTableMigrated(string tableName, UniqueConstraintDbContext context);

    /// <summary>
    /// Clears cached migration state for all tables whose connection string starts with the given prefix.
    /// </summary>
    /// <param name="connectionStringPrefix">
    /// Connection string prefix to match. Pass an empty string to clear all cached entries.
    /// </param>
    void ClearMigrationCache(string connectionStringPrefix);
}
