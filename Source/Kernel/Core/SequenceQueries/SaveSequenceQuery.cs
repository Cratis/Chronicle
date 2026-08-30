// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.SequenceQueries;

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
/// <param name="EventSourceType">The event source type to narrow to, or empty for every event source type.</param>
/// <param name="EventStreamType">The event stream type to narrow to, or empty for every event stream type.</param>
/// <param name="CorrelationId">The correlation to narrow to, or empty for every correlation.</param>
/// <param name="EventTypes">The event type identifiers to narrow to, or empty for every event type.</param>
/// <param name="Tags">The tags to narrow to, or empty for every event.</param>
/// <param name="OccurredFrom">The inclusive lower bound on when the event occurred.</param>
/// <param name="OccurredTo">The exclusive upper bound on when the event occurred.</param>
/// <param name="SortBy">What the results are ordered by.</param>
/// <param name="Descending">Whether results are ordered from the highest value down rather than from the lowest up.</param>
/// <remarks>
/// Replaces the whole query every time rather than patching it, so the caller always sends the
/// complete state it wants persisted.
/// </remarks>
[Command]
public record SaveSequenceQuery(
    EventStoreName EventStore,
    SequenceQueryId Id,
    string Name,
    SequenceQueryScope Scope,
    string Folder,
    EventStoreNamespaceName Namespace,
    string EventSequenceId,
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string CorrelationId,
    IEnumerable<string> EventTypes,
    IEnumerable<string> Tags,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo,
    string SortBy,
    bool Descending)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the owner.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(ICurrentPrincipalAccessor currentPrincipalAccessor, IStorage storage) =>
        storage.GetEventStore(EventStore).SequenceQueries.Save(
            new SequenceQueryDefinition(
                Id,
                Name,
                Scope,

                // Ownership follows the principal saving the query, never a value the client supplies,
                // so a caller cannot plant a query into somebody else's private set.
                SequenceQueryOwners.GetCurrent(currentPrincipalAccessor),
                Folder,
                Namespace,
                EventSequenceId,
                new SequenceQueryFilter(
                    EventSourceId,
                    EventSourceType,
                    EventStreamType,
                    CorrelationId,
                    [.. EventTypes],
                    [.. Tags],
                    OccurredFrom,
                    OccurredTo),
                SequenceQuerySortByParser.Parse(SortBy),
                Descending));
}
