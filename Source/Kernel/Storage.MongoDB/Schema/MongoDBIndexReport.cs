// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// The result of inspecting the MongoDB indexes owned by Chronicle.
/// </summary>
/// <param name="Scopes">The inspected scopes.</param>
public record MongoDBIndexReport(IReadOnlyList<MongoDBIndexScopeResult> Scopes)
{
    /// <summary>
    /// Gets whether every classified required index exists and obsolete indexes have been removed.
    /// Unclassified constraint collections are advisory and do not affect this value.
    /// </summary>
    public bool IsCurrent => Scopes.All(scope => scope.Missing.Count == 0 && scope.Obsolete.Count == 0);
}
