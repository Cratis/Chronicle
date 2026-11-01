// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// One resolved read model database: how to connect to it, and what else shares it.
/// </summary>
internal abstract class ReadModelTablePlace
{
    /// <summary>
    /// Gets where the database physically is, or null when it cannot be told.
    /// </summary>
    internal abstract ReadModelTablePlacement? Placement { get; }

    /// <summary>
    /// Gets the resolved schema.
    /// </summary>
    internal abstract string Schema { get; }

    /// <summary>
    /// Gets the migrator for the name ledger.
    /// </summary>
    internal abstract ITableMigrator<ReadModelTableBindingsDbContext> LedgerMigrator { get; }

    /// <summary>
    /// Opens a prepared context for a physical table; the database exists and so does the table, unless it is required to.
    /// </summary>
    /// <param name="physicalName">The physical table name.</param>
    /// <param name="columns">The columns of the read model.</param>
    /// <param name="requireExisting">Whether a missing table is an error instead of being created.</param>
    /// <returns>The context, owned by the caller.</returns>
    internal abstract Task<ReadModelDbContext> OpenTable(string physicalName, IReadOnlyList<ProjectedColumn> columns, bool requireExisting);

    /// <summary>
    /// Opens the name ledger context at the schema identity, after making sure the database exists.
    /// </summary>
    /// <returns>The context, owned by the caller.</returns>
    internal abstract Task<ReadModelTableBindingsDbContext> OpenLedger();

    /// <summary>
    /// Enumerates the metadata of every scope that could own a name in this database.
    /// </summary>
    /// <returns>The scopes, owned by the caller.</returns>
    internal abstract Task<IReadOnlyList<ReadModelTableMetadataScope>> Scopes();
}
