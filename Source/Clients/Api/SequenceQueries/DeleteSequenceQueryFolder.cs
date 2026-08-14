// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.SequenceQueries;

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the command for deleting a folder from the saved query hierarchy.
/// </summary>
/// <param name="EventStore">The event store the folder belongs to.</param>
/// <param name="Id">The unique identifier of the folder to delete.</param>
[Command]
public record DeleteSequenceQueryFolder(string EventStore, string Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="sequenceQueries">The <see cref="ISequenceQueries"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(ISequenceQueries sequenceQueries) =>
        sequenceQueries.DeleteFolder(new()
        {
            EventStore = EventStore,
            Id = Id
        });
}
