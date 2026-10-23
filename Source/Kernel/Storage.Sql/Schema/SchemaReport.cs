// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The result of visiting every reachable versioned schema.
/// </summary>
/// <param name="Scopes">Results for individual scopes.</param>
public record SchemaReport(IReadOnlyList<SchemaScopeResult> Scopes)
{
    /// <summary>
    /// Gets whether every visited scope is current.
    /// </summary>
    public bool IsCurrent => Scopes.All(scope => scope.Pending.Count == 0);
}
