// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Represents a folder in the saved event sequence query hierarchy.
/// </summary>
/// <param name="Id">The unique identifier of the folder.</param>
/// <param name="Scope">Who the folder is visible to.</param>
/// <param name="Owner">The identity that created it.</param>
/// <param name="Namespace">The namespace the folder belongs to.</param>
/// <param name="Path">Where the folder sits within its scope.</param>
/// <remarks>
/// Folders are stored in their own right rather than inferred from the paths queries carry, so that
/// one can be created before there is anything to put in it.
/// </remarks>
[ReadModel]
public record QueryFolder(
    string Id,
    SequenceQueryScope Scope,
    string Owner,
    string Namespace,
    string Path)
{
    /// <summary>
    /// Gets the folders the current identity can see - their own, plus the ones shared with everyone.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the owner.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>A collection of <see cref="QueryFolder"/>.</returns>
    public static async Task<IEnumerable<QueryFolder>> AllQueryFolders(
        string eventStore,
        ICurrentPrincipalAccessor currentPrincipalAccessor,
        IStorage storage)
    {
        var owner = SequenceQueryOwners.GetCurrent(currentPrincipalAccessor);
        var folders = await storage.GetEventStore(eventStore).SequenceQueries.GetAllFoldersFor(owner);

        return folders.ToReadModel();
    }
}
