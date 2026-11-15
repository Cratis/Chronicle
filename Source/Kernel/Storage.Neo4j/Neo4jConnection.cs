// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents a resolved connection to the Neo4j database of a graph.
/// </summary>
/// <param name="Driver">The shared, pooled <see cref="IDriver"/>.</param>
/// <param name="Database">The name of the Neo4j database.</param>
/// <param name="CanCreateDatabase">Whether Chronicle owns the placement of the database, and so may create it when it is missing.</param>
public record Neo4jConnection(IDriver Driver, string Database, bool CanCreateDatabase)
{
    /// <summary>
    /// The database every Neo4j server has, which administration commands run against.
    /// </summary>
    public const string SystemDatabase = "system";

    /// <summary>
    /// Open a session on the database of the graph.
    /// </summary>
    /// <param name="accessMode">The <see cref="AccessMode"/> of the session.</param>
    /// <returns>The <see cref="IAsyncSession"/>.</returns>
    public IAsyncSession OpenSession(AccessMode accessMode = AccessMode.Write) => OpenSession(Database, accessMode);

    /// <summary>
    /// Open a session on the system database, for administration.
    /// </summary>
    /// <returns>The <see cref="IAsyncSession"/>.</returns>
    public IAsyncSession OpenSystemSession() => OpenSession(SystemDatabase, AccessMode.Write);

    IAsyncSession OpenSession(string database, AccessMode accessMode) =>
        Driver.AsyncSession(_ => _.WithDatabase(database).WithDefaultAccessMode(accessMode));
}
