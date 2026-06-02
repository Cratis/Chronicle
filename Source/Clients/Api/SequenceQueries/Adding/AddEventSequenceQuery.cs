// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.SequenceQueries.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Api.SequenceQueries.Adding;

/// <summary>
/// Command for adding an event sequence query to a folder. The folder's ownership transitively defines
/// the query's ownership through the projection's parent/child relationship.
/// </summary>
/// <param name="EventStore">The event store context.</param>
/// <param name="Namespace">The namespace context.</param>
/// <param name="QueryId">The unique identifier of the query.</param>
/// <param name="FolderId">The folder this query belongs to.</param>
/// <param name="Name">The display name of the query.</param>
/// <param name="EventSequenceId">The event sequence the query targets.</param>
/// <param name="Filter">The filter definition for the query.</param>
[Command]
public record AddEventSequenceQuery(
    string EventStore,
    string Namespace,
    [property: Key] SequenceQueryId QueryId,
    EventSequenceQueryFolderId FolderId,
    string Name,
    string EventSequenceId,
    SequenceQueryFilter Filter)
{
    /// <summary>
    /// Handles the command by emitting a query-added event.
    /// </summary>
    /// <returns>The <see cref="EventSequenceQueryAdded"/> event to append.</returns>
    internal EventSequenceQueryAdded Handle() => new(FolderId, Name, EventSequenceId, Filter);
}
