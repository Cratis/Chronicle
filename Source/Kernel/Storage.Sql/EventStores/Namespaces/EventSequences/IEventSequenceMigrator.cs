// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Defines a system for managing migrations for event sequence tables.
/// </summary>
public interface IEventSequenceMigrator
{
    /// <summary>
    /// Ensures the event sequence table exists and has the correct schema.
    /// </summary>
    /// <param name="tableName">The name of the event sequence table.</param>
    /// <param name="context">The <see cref="EventSequenceDbContext"/> for the table.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureTableMigrated(string tableName, EventSequenceDbContext context);
}
