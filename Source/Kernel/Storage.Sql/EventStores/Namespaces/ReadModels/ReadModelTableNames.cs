// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Derives length-safe candidate names for read model tables, without establishing ownership.
/// </summary>
internal static class ReadModelTableNames
{
    /// <summary>
    /// Keeps logical names within the provider limit unchanged and shortens longer names deterministically.
    /// The result must be resolved against durable ownership before addressing a table.
    /// </summary>
    /// <param name="databaseType">The SQL provider.</param>
    /// <param name="name">The full logical table name.</param>
    /// <returns>A candidate table name, not a resolved address.</returns>
    internal static string Candidate(DatabaseType databaseType, string name) =>
        databaseType is DatabaseType.PostgreSql or DatabaseType.SqlServer ? DatabaseName.Physical(name, databaseType) : name;

    /// <summary>
    /// Gets the table name stored by earlier versions, including PostgreSQL truncation.
    /// </summary>
    /// <param name="databaseType">The SQL provider.</param>
    /// <param name="name">The logical table name.</param>
    /// <returns>The legacy stored table name.</returns>
    internal static string Legacy(DatabaseType databaseType, string name) => PrimaryKeyNames.TableIdentifier(databaseType, name);

    /// <summary>
    /// Tests whether PostgreSQL could have stored a different, truncated legacy name.
    /// </summary>
    /// <param name="databaseType">The SQL provider.</param>
    /// <param name="name">The logical table name.</param>
    /// <returns>Whether a distinct legacy table name is possible.</returns>
    internal static bool HasLegacyName(DatabaseType databaseType, string name) =>
        databaseType == DatabaseType.PostgreSql && Legacy(databaseType, name) != Candidate(databaseType, name);

    /// <summary>
    /// Gets the logical replay shadow name.
    /// </summary>
    /// <param name="containerName">The logical read model container name.</param>
    /// <returns>The logical replay table name.</returns>
    internal static string Replay(string containerName) => $"replay-{containerName}";
}
