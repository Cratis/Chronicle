// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The exception thrown when a dynamic schema change requires explicit provisioning.
/// </summary>
/// <param name="kind">The kind of object.</param>
/// <param name="name">The object name.</param>
/// <param name="database">The database name, without connection credentials.</param>
public sealed class DynamicSchemaObjectNotProvisioned(string kind, string name, string database)
    : Exception($"The {kind} '{name}' in database '{database}' is not provisioned. Run chronicle --migrate-only before starting in Verify mode.")
{
    /// <summary>
    /// Gets the database requiring provisioning, without connection credentials.
    /// </summary>
    internal string Database { get; } = database;
}
