// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Defines how read model tables are opened by logical identity and how their name bindings move through their lifecycle.
/// </summary>
internal interface IReadModelTables
{
    /// <summary>
    /// Opens a table, reserving, creating and binding it first when it is a new live or replay table.
    /// </summary>
    /// <param name="handle">The logical identity.</param>
    /// <param name="columns">The columns of the read model.</param>
    /// <param name="purpose">The connection purpose.</param>
    /// <returns>The scope; its context addresses the bound physical name.</returns>
    Task<DbContextScope<ReadModelDbContext>> Open(ReadModelTableHandle handle, IReadOnlyList<ProjectedColumn> columns, ConnectionPurpose purpose = ConnectionPurpose.Runtime);

    /// <summary>
    /// Reserves a physical name for a table without creating the table.
    /// </summary>
    /// <param name="handle">The logical identity.</param>
    /// <returns>The binding.</returns>
    Task<ReadModelTableNameBinding> Reserve(ReadModelTableHandle handle);

    /// <summary>
    /// Finds an existing binding without creating one.
    /// </summary>
    /// <param name="handle">The logical identity.</param>
    /// <returns>The binding, or null.</returns>
    Task<ReadModelTableNameBinding?> Find(ReadModelTableHandle handle);

    /// <summary>
    /// Finds a retained backup by occurrence, even if the read model's current container changed.
    /// </summary>
    /// <param name="handle">The requested scope, model, and full logical backup name.</param>
    /// <returns>The unique binding, or null when none exists.</returns>
    Task<ReadModelTableNameBinding?> FindBackup(ReadModelTableHandle handle);

    /// <summary>
    /// Moves a binding between states with compare-and-set.
    /// </summary>
    /// <param name="handle">The logical identity.</param>
    /// <param name="expected">The state the binding must be in.</param>
    /// <param name="next">The state to move it to.</param>
    /// <param name="enlist">A model context whose current transaction the change joins, if any.</param>
    /// <returns>Whether the change applied.</returns>
    Task<bool> Transition(ReadModelTableHandle handle, ReadModelTableBindingState expected, ReadModelTableBindingState next, DbContext? enlist = null);

    /// <summary>
    /// Changes the replay attempt with compare-and-set, optionally in the promotion transaction.
    /// </summary>
    /// <param name="handle">The replay table identity.</param>
    /// <param name="expected">The expected current attempt, or null.</param>
    /// <param name="attempt">The new attempt, or null to clear it.</param>
    /// <param name="enlist">The transaction to join, if any.</param>
    /// <returns>Whether the change applied.</returns>
    Task<bool> SetReplayAttempt(ReadModelTableHandle handle, string? expected, string? attempt, DbContext? enlist = null);

    /// <summary>
    /// Forgets everything resolved so far.
    /// </summary>
    void Clear();
}
