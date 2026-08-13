// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle.Contracts.SequenceQueries;

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the command for saving an event sequence query.
/// </summary>
/// <param name="EventStore">The event store the query belongs to.</param>
/// <param name="Id">The unique identifier of the query.</param>
/// <param name="Name">The display name the user gave it.</param>
/// <param name="Scope">Who the query should be visible to.</param>
/// <param name="Folder">The folder within the scope to file the query under, or empty to place it directly under its scope.</param>
/// <param name="Namespace">The namespace the query runs against.</param>
/// <param name="EventSequenceId">The event sequence the query runs against.</param>
/// <param name="EventSourceId">The event source to narrow to, or empty for every event source.</param>
/// <param name="EventTypes">The event type identifiers to narrow to, or empty for every event type.</param>
/// <param name="Tags">The tags to narrow to, or empty for every event.</param>
/// <param name="OccurredFrom">The inclusive lower bound on when the event occurred.</param>
/// <param name="OccurredTo">The exclusive upper bound on when the event occurred.</param>
/// <param name="Descending">Whether results are ordered newest first.</param>
/// <remarks>
/// Replaces the whole query every time rather than patching it, so the caller always sends the
/// complete state it wants persisted.
/// </remarks>
[Command]
public record SaveSequenceQuery(
    string EventStore,
    string Id,
    string Name,
    SequenceQueryScope Scope,
    string Folder,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    IEnumerable<string> EventTypes,
    IEnumerable<string> Tags,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo,
    bool Descending)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="sequenceQueries">The <see cref="ISequenceQueries"/> contract.</param>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the owner.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(ISequenceQueries sequenceQueries, ICurrentPrincipalAccessor currentPrincipalAccessor) =>
        sequenceQueries.Save(new()
        {
            EventStore = EventStore,
            Query = new()
            {
                Id = Id,
                Name = Name,
                Scope = (Contracts.SequenceQueries.SequenceQueryScope)Scope,

                // Ownership follows the principal saving the query, never a value the client supplies,
                // so a caller cannot plant a query into somebody else's private set.
                Owner = SequenceQueryOwners.GetCurrent(currentPrincipalAccessor),
                Folder = Folder,
                Namespace = Namespace,
                EventSequenceId = EventSequenceId,
                EventSourceId = EventSourceId,
                EventTypes = [.. EventTypes],
                Tags = [.. Tags],
                OccurredFrom = OccurredFrom,
                OccurredTo = OccurredTo,
                Descending = Descending
            }
        });
}
