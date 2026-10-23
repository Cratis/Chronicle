// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Provisions or verifies SQL schemas without starting a kernel host.
/// </summary>
public interface ISchemaProvisioner
{
    /// <summary>
    /// Provisions all known scopes plus the requested scopes.
    /// </summary>
    /// <param name="scope">Additional scopes.</param>
    /// <returns>The schema report.</returns>
    Task<SchemaReport> Migrate(SchemaScope scope);

    /// <summary>
    /// Reads schema history and metadata without applying DDL or writing registries.
    /// </summary>
    /// <param name="scope">Additional scopes.</param>
    /// <returns>The schema report.</returns>
    Task<SchemaReport> Verify(SchemaScope scope);
}
