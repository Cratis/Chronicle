// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Represents an event sequence query a user saved so it can be reopened later.
/// </summary>
/// <param name="Id">The unique identifier of the query.</param>
/// <param name="Name">The display name the user gave it.</param>
/// <param name="Scope">Who the query is visible to.</param>
/// <param name="Owner">The identity that saved it.</param>
/// <param name="Folder">The folder within the scope the query is filed under, or empty when it sits directly under its scope.</param>
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
[ReadModel]
public record SequenceQuery(
    string Id,
    string Name,
    SequenceQueryScope Scope,
    string Owner,
    string Folder,
    string Namespace,
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
    /// Gets the saved queries the current identity can see - their own, plus the ones shared with everyone.
    /// </summary>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the owner.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>A collection of <see cref="SequenceQuery"/>.</returns>
    /// <remarks>
    /// A snapshot rather than an observable: which queries an identity may see depends on the
    /// principal executing the call, and the caller re-reads after saving or deleting anyway.
    /// </remarks>
    public static async Task<IEnumerable<SequenceQuery>> AllSequenceQueries(
        string eventStore,
        ICurrentPrincipalAccessor currentPrincipalAccessor,
        IStorage storage)
    {
        var owner = SequenceQueryOwners.GetCurrent(currentPrincipalAccessor);
        var queries = await storage.GetEventStore(eventStore).SequenceQueries.GetAllFor(owner);

        return queries.ToReadModel();
    }
}
