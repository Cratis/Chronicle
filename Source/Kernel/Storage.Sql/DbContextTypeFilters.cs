// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Extension methods for filtering DbContext types.
/// </summary>
public static class DbContextTypeFilters
{
    /// <summary>
    /// Filters the DbContext types to only include those in the Chronicle namespace.
    /// </summary>
    /// <param name="types">The types to filter.</param>
    /// <returns>The filtered types.</returns>
    public static IEnumerable<Type> OnlyChronicle(this IEnumerable<Type> types) => types
        .Where(t => t.Namespace?.StartsWith(typeof(StorageType).Namespace!) ?? false)
        .ToArray();
}
