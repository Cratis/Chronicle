// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Represents the command for saving a folder in the saved query hierarchy.
/// </summary>
/// <param name="EventStore">The event store the folder belongs to.</param>
/// <param name="Id">The unique identifier of the folder.</param>
/// <param name="Scope">Who the folder should be visible to.</param>
/// <param name="Namespace">The namespace the folder belongs to.</param>
/// <param name="Path">Where the folder sits within its scope.</param>
/// <remarks>
/// Replaces the whole folder every time rather than patching it, so renaming or moving one is the
/// same call as creating it.
/// </remarks>
[Command]
public record SaveSequenceQueryFolder(
    EventStoreName EventStore,
    SequenceQueryFolderId Id,
    SequenceQueryScope Scope,
    EventStoreNamespaceName Namespace,
    string Path)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the owner.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(ICurrentPrincipalAccessor currentPrincipalAccessor, IStorage storage) =>
        storage.GetEventStore(EventStore).SequenceQueries.SaveFolder(
            new SequenceQueryFolderDefinition(
                Id,
                Scope,

                // Ownership follows the principal creating the folder, never a value the client
                // supplies, so a caller cannot plant a folder into somebody else's private set.
                SequenceQueryOwners.GetCurrent(currentPrincipalAccessor),
                Namespace,
                Path));
}
