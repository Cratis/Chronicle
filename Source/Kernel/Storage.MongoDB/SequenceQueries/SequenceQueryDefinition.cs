// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries;

/// <summary>
/// Represents the MongoDB document for a saved event sequence query.
/// </summary>
/// <param name="Id">The unique identifier of the query - the primary key.</param>
/// <param name="Name">The display name the user gave it.</param>
/// <param name="Scope">Who the query is visible to.</param>
/// <param name="Owner">The identity that saved it.</param>
/// <param name="Namespace">The namespace the query runs against.</param>
/// <param name="EventSequenceId">The event sequence the query runs against.</param>
/// <param name="EventSourceId">The event source to narrow to, or empty for every event source.</param>
/// <param name="EventTypes">The event type identifiers to narrow to, or empty for every event type.</param>
/// <param name="Tags">The tags to narrow to, or empty for every event.</param>
/// <param name="OccurredFrom">The inclusive lower bound on when the event occurred.</param>
/// <param name="OccurredTo">The exclusive upper bound on when the event occurred.</param>
/// <param name="Descending">Whether results are ordered newest first.</param>
public record SequenceQueryDefinition(
    string Id,
    string Name,
    SequenceQueryScope Scope,
    string Owner,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    IEnumerable<string> EventTypes,
    IEnumerable<string> Tags,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo,
    bool Descending);
