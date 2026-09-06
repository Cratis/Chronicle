// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.SequenceQueries;

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the command for deleting a saved event sequence query.
/// </summary>
/// <param name="EventStore">The event store the query belongs to.</param>
/// <param name="Id">The unique identifier of the query to delete.</param>
[Command]
public record DeleteSequenceQuery(string EventStore, string Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="sequenceQueries">The <see cref="ISequenceQueries"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(ISequenceQueries sequenceQueries) =>
        sequenceQueries.Delete(new()
        {
            EventStore = EventStore,
            Id = Id
        });
}
