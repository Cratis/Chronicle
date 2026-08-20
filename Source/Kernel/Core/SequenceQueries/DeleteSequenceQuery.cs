// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.SequenceQueries;

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
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IStorage storage) =>
        storage.GetEventStore(EventStore).SequenceQueries.Delete(new SequenceQueryId(Id));
}
