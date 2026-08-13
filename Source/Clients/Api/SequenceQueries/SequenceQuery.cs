// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle.Contracts.SequenceQueries;

namespace Cratis.Chronicle.Api.SequenceQueries;

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
/// <param name="EventTypes">The event type identifiers to narrow to, or empty for every event type.</param>
/// <param name="Tags">The tags to narrow to, or empty for every event.</param>
/// <param name="OccurredFrom">The inclusive lower bound on when the event occurred.</param>
/// <param name="OccurredTo">The exclusive upper bound on when the event occurred.</param>
/// <param name="Descending">Whether results are ordered newest first.</param>
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
    IEnumerable<string> EventTypes,
    IEnumerable<string> Tags,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo,
    bool Descending)
{
    /// <summary>
    /// Gets the saved queries the current identity can see - their own, plus the ones shared with everyone.
    /// </summary>
    /// <param name="sequenceQueries"><see cref="ISequenceQueries"/> for working with saved queries.</param>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the owner.</param>
    /// <param name="eventStore">Event store to get for.</param>
    /// <returns>A collection of <see cref="SequenceQuery"/>.</returns>
    /// <remarks>
    /// A snapshot rather than an observable: which queries an identity may see depends on the
    /// principal executing the call, and the caller re-reads after saving or deleting anyway.
    /// </remarks>
    public static async Task<IEnumerable<SequenceQuery>> AllSequenceQueries(
        ISequenceQueries sequenceQueries,
        ICurrentPrincipalAccessor currentPrincipalAccessor,
        string eventStore)
    {
        var owner = SequenceQueryOwners.GetCurrent(currentPrincipalAccessor);
        var queries = await sequenceQueries.GetSequenceQueries(new() { EventStore = eventStore, Owner = owner });

        return queries.Select(ToApi).ToArray();
    }

    static SequenceQuery ToApi(SequenceQueryDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            (SequenceQueryScope)definition.Scope,
            definition.Owner,
            definition.Folder,
            definition.Namespace,
            definition.EventSequenceId,
            definition.EventSourceId,
            definition.EventTypes,
            definition.Tags,
            definition.OccurredFrom,
            definition.OccurredTo,
            definition.Descending);
}
