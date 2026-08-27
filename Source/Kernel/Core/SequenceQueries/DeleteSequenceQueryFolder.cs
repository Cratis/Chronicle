// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Represents the command for deleting a folder from the saved query hierarchy.
/// </summary>
/// <param name="EventStore">The event store the folder belongs to.</param>
/// <param name="Id">The unique identifier of the folder to delete.</param>
[Command]
public record DeleteSequenceQueryFolder(EventStoreName EventStore, SequenceQueryFolderId Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IStorage storage) =>
        storage.GetEventStore(EventStore).SequenceQueries.DeleteFolder(Id);
}
