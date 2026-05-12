// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Defines a service for migrating read model tables in a SQL database.
/// </summary>
public interface IReadModelMigrator
{
    /// <summary>
    /// Ensures that the table has been created for the given read model container.
    /// </summary>
    /// <param name="tableName">The name of the table to ensure exists.</param>
    /// <param name="context">The <see cref="ReadModelDbContext"/> instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EnsureTableMigrated(string tableName, ReadModelDbContext context);
}
