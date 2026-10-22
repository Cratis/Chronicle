// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the storage configuration.
/// </summary>
public class Storage
{
    /// <summary>
    /// Gets the type of storage used.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the connection details for the storage.
    /// </summary>
    public string ConnectionDetails { get; init; } = string.Empty;

    /// <summary>
    /// Gets the prefix prepended to every MongoDB database name. Empty preserves the default names.
    /// </summary>
    public string DatabaseNamePrefix { get; init; } = string.Empty;

    /// <summary>
    /// Gets the SQL schema Chronicle keeps its tables in. Empty uses the default schema of the database user.
    /// </summary>
    /// <remarks>
    /// PostgreSQL connects with the schema first on its search path. SQL Server has no per-connection schema, so the
    /// database users Chronicle connects as must have it as their default schema; Chronicle verifies that before
    /// migrating. MongoDB and SQLite have no schemas and ignore it. A resolved schema takes precedence.
    /// </remarks>
    public string Schema { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether Chronicle creates databases and schemas that do not exist.
    /// </summary>
    /// <remarks>
    /// Turn it off when a platform provisions and places databases. A missing database then fails with
    /// a clear error instead of being created. Migrations still run inside existing databases.
    /// </remarks>
    public bool CreateIfMissing { get; init; } = true;

    /// <summary>
    /// Gets native Microsoft Entra authentication for PostgreSQL. Disabled by default.
    /// </summary>
    public PostgreSqlEntra PostgreSqlEntra { get; init; } = new();

    /// <summary>
    /// Gets how connections are resolved per event store, namespace and purpose.
    /// </summary>
    /// <remarks>
    /// When nothing is configured, every connection is derived from <see cref="ConnectionDetails"/> as before.
    /// </remarks>
    public ConnectionResolutionOptions ConnectionResolution { get; init; } = new();
}
