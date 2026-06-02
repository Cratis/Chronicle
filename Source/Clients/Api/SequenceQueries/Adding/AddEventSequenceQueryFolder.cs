// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.SequenceQueries.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Api.SequenceQueries.Adding;

/// <summary>
/// Command for adding a shared (system-owned) event sequence query folder. The returned event is
/// auto-appended by the kernel server's command response value handlers; the folder identifier acts
/// as the event source.
/// </summary>
/// <param name="EventStore">The event store context.</param>
/// <param name="Namespace">The namespace context.</param>
/// <param name="FolderId">The unique identifier of the folder.</param>
/// <param name="Name">The display name of the folder.</param>
[Command]
public record AddEventSequenceQueryFolder(
    string EventStore,
    string Namespace,
    [property: Key] EventSequenceQueryFolderId FolderId,
    string Name)
{
    /// <summary>
    /// Handles the command by emitting a folder-added event.
    /// </summary>
    /// <returns>The <see cref="EventSequenceQueryFolderAdded"/> event to append.</returns>
    internal EventSequenceQueryFolderAdded Handle() => new(Name);
}
