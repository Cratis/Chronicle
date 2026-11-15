// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Security;
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
    /// <param name="administratorAuthorization">The <see cref="IAdministratorAuthorization"/> for the administrator override.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>Null when the change was applied, otherwise the refusal.</returns>
    public async Task<ValidationResult?> Handle(ICurrentPrincipalAccessor currentPrincipalAccessor, IAdministratorAuthorization administratorAuthorization, IStorage storage)
    {
        var queries = storage.GetEventStore(EventStore).SequenceQueries;

        return await SequenceQueryChangeAccess.Change(
            () => queries.GetFolder(Id),
            existing => existing.Owner,
            existing => existing is null
                ? queries.InsertFolder(Definition(SequenceQueryOwners.GetCurrent(currentPrincipalAccessor)))

                // Ownership follows the principal creating the folder, never a value the client
                // supplies, so a caller cannot plant a folder into somebody else's private set.
                // Updating an existing folder keeps the owner it is stored under, and only while it still is.
                : queries.UpdateFolder(Definition(existing.Owner), existing.Owner),
            SequenceQueryChangeAccess.FolderRefusal,
            currentPrincipalAccessor,
            administratorAuthorization);
    }

    SequenceQueryFolderDefinition Definition(SequenceQueryOwner owner) => new(Id, Scope, owner, Namespace, Path);
}
