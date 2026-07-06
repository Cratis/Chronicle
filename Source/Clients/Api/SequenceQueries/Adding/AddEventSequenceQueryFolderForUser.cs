// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.SequenceQueries.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Api.SequenceQueries.Adding;

/// <summary>
/// Command for adding an event sequence query folder owned by the calling user. The user identity is
/// taken from <c>EventContext.CausedBy</c> at projection time, so the command itself carries no owner
/// information.
/// </summary>
/// <param name="EventStore">The event store context.</param>
/// <param name="Namespace">The namespace context.</param>
/// <param name="FolderId">The unique identifier of the folder.</param>
/// <param name="Name">The display name of the folder.</param>
[Command]
public record AddEventSequenceQueryFolderForUser(
    string EventStore,
    string Namespace,
    [property: Key] EventSequenceQueryFolderId FolderId,
    string Name)
{
    /// <summary>
    /// Handles the command by emitting a user-owned-folder-added event.
    /// </summary>
    /// <returns>The <see cref="EventSequenceQueryFolderForUserAdded"/> event to append.</returns>
    internal EventSequenceQueryFolderForUserAdded Handle() => new(Name);
}
