// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle.Contracts.SequenceQueries;

namespace Cratis.Chronicle.Api.SequenceQueries;

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
    string EventStore,
    string Id,
    SequenceQueryScope Scope,
    string Namespace,
    string Path)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="sequenceQueries">The <see cref="ISequenceQueries"/> contract.</param>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the owner.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(ISequenceQueries sequenceQueries, ICurrentPrincipalAccessor currentPrincipalAccessor) =>
        sequenceQueries.SaveFolder(new()
        {
            EventStore = EventStore,
            Folder = new()
            {
                Id = Id,
                Scope = (Contracts.SequenceQueries.SequenceQueryScope)Scope,

                // Ownership follows the principal creating the folder, never a value the client
                // supplies, so a caller cannot plant a folder into somebody else's private set.
                Owner = SequenceQueryOwners.GetCurrent(currentPrincipalAccessor),
                Namespace = Namespace,
                Path = Path
            }
        });
}
