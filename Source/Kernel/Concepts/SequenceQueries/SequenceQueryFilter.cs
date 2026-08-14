// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents the narrowing a user configured on a saved event sequence query.
/// </summary>
/// <param name="EventSourceId">The event source to narrow to, or empty for every event source.</param>
/// <param name="EventSourceType">The event source type to narrow to, or empty for every event source type.</param>
/// <param name="EventStreamType">The event stream type to narrow to, or empty for every event stream type.</param>
/// <param name="CorrelationId">The correlation to narrow to, or empty for every correlation.</param>
/// <param name="EventTypes">The event type identifiers to narrow to, or empty for every event type.</param>
/// <param name="Tags">The tags to narrow to - an event matches when it carries any of them - or empty for every event.</param>
/// <param name="OccurredFrom">The inclusive lower bound on when the event occurred, or null for unbounded.</param>
/// <param name="OccurredTo">The exclusive upper bound on when the event occurred, or null for unbounded.</param>
public record SequenceQueryFilter(
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string CorrelationId,
    IEnumerable<string> EventTypes,
    IEnumerable<string> Tags,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo)
{
    /// <summary>
    /// Represents a filter that narrows nothing.
    /// </summary>
    public static readonly SequenceQueryFilter Empty = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        [],
        [],
        null,
        null);
}
