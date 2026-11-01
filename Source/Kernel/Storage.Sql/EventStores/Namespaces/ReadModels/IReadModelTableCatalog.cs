// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Exact catalog probes and identifier equality for one resolved database/schema.
/// </summary>
internal interface IReadModelTableCatalog
{
    /// <summary>Gets the SQL provider whose identifier rules apply.</summary>
    DatabaseType Provider { get; }

    /// <summary>
    /// Finds any relation occupying this identifier, returning its actual catalog spelling.
    /// </summary>
    /// <param name="name">The complete name, never an untyped SQL literal.</param>
    /// <returns>The stored name or null.</returns>
    Task<string?> Find(string name);

    /// <summary>
    /// Compares identifiers with the database's table-identifier equality.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns>Whether they occupy the same name.</returns>
    Task<bool> Equal(string left, string right);
}
