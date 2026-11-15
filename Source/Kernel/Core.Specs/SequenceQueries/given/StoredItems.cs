// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.given;

/// <summary>
/// Builds stored queries and folders for specs that change what the storage holds between reads.
/// </summary>
public static class StoredItems
{
    /// <summary>
    /// Build a stored query.
    /// </summary>
    /// <param name="owner">The owner it is stored under.</param>
    /// <returns>The stored <see cref="Concepts.SequenceQueries.SequenceQueryDefinition"/>.</returns>
    public static Concepts.SequenceQueries.SequenceQueryDefinition Query(string owner) =>
        new(
            "query-1",
            "Stored",
            SequenceQueryScope.User,
            owner,
            string.Empty,
            "default",
            "event-log",
            Concepts.SequenceQueries.SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false);

    /// <summary>
    /// Build a stored folder.
    /// </summary>
    /// <param name="owner">The owner it is stored under.</param>
    /// <returns>The stored <see cref="SequenceQueryFolderDefinition"/>.</returns>
    public static SequenceQueryFolderDefinition Folder(string owner) =>
        new("folder-1", SequenceQueryScope.User, owner, "default", "stored");
}
