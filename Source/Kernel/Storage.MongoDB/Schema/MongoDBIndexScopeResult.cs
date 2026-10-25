// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// The index state of one logical storage scope.
/// </summary>
/// <param name="Scope">The logical scope.</param>
/// <param name="Database">The physical database name.</param>
/// <param name="Missing">Missing or incompatible collection/index names.</param>
/// <param name="Obsolete">Obsolete collection/index names.</param>
public record MongoDBIndexScopeResult(string Scope, string Database, IReadOnlyList<string> Missing, IReadOnlyList<string> Obsolete)
{
    /// <summary>
    /// Gets existing constraint collections whose stored definitions do not select a single mode.
    /// These are advisory and do not make the report non-current.
    /// </summary>
    public IReadOnlyList<string> Unclassified { get; init; } = [];
}
