// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Builds the criteria that narrow an event sequence query from the values a workbench query carries.
/// </summary>
public static class EventSequenceQueryCriteriaFactory
{
    /// <summary>
    /// Create criteria from the narrowing values of a workbench query.
    /// </summary>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="occurredFrom">Optional inclusive lower bound on when the event occurred.</param>
    /// <param name="occurredTo">Optional exclusive upper bound on when the event occurred.</param>
    /// <param name="tags">Optional comma separated tags to narrow to.</param>
    /// <returns>The <see cref="EventSequenceQueryCriteria"/>.</returns>
    /// <remarks>
    /// A blank value leaves its dimension unnarrowed rather than turning into an empty-string match,
    /// which is what a query with no filters configured sends.
    /// </remarks>
    public static EventSequenceQueryCriteria Create(
        string? eventSourceId,
        string? eventTypeIds,
        DateTimeOffset? occurredFrom,
        DateTimeOffset? occurredTo,
        string? tags = default) =>
        new()
        {
            EventSourceId = string.IsNullOrWhiteSpace(eventSourceId) ? null : eventSourceId,
            EventTypes = [.. Split(eventTypeIds).Select(id => new EventType { Id = id, Generation = 1 })],
            Tags = [.. Split(tags)],
            OccurredFrom = occurredFrom,
            OccurredTo = occurredTo
        };

    static string[] Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
