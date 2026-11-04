// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Storage.Diagnostics;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Represents a storage operation the kernel's storage user was refused.
/// </summary>
/// <param name="Database">The database name.</param>
/// <param name="Collection">The collection name.</param>
/// <param name="Operation">The refused operation name.</param>
[ReadModel]
public record StorageAuthorizationDiagnostic(string Database, string Collection, string Operation)
{
    /// <summary>
    /// Gets the outstanding storage authorization failures.
    /// </summary>
    /// <param name="diagnostics">The <see cref="IStorageAuthorizationDiagnostics"/>.</param>
    /// <returns>The failures, one per refused operation.</returns>
    public static IEnumerable<StorageAuthorizationDiagnostic> AllStorageAuthorizationDiagnostics(IStorageAuthorizationDiagnostics diagnostics) =>
        diagnostics.Failures.Select(_ => new StorageAuthorizationDiagnostic(_.Database, _.Collection, _.Operation));
}
