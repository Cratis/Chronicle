// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Builds the criteria that narrow an event sequence query from the values a workbench query carries.
/// </summary>
public static class EventSequenceQueryCriteriaFactory
{
    /// <summary>
    /// Create criteria from the narrowing values of a workbench query.
    /// </summary>
    /// <param name="narrowing">The <see cref="EventSequenceQueryNarrowing"/> the caller asked for.</param>
    /// <returns>The <see cref="EventSequenceQueryCriteria"/>.</returns>
    /// <remarks>
    /// A blank value leaves its dimension unnarrowed rather than turning into an empty-string match,
    /// which is what a query with no filters configured sends.
    /// </remarks>
    public static EventSequenceQueryCriteria Create(EventSequenceQueryNarrowing narrowing) =>
        new()
        {
            EventSourceId = Trimmed(narrowing.EventSourceId),
            EventSourceType = Trimmed(narrowing.EventSourceType),
            EventStreamType = Trimmed(narrowing.EventStreamType),
            CorrelationId = Guid.TryParse(narrowing.CorrelationId, out var correlationId) ? correlationId : null,
            EventTypes = [.. Split(narrowing.EventTypeIds).Select(id => new Contracts.Events.EventType { Id = id, Generation = 1 })],
            Tags = [.. Split(narrowing.Tags)],
            OccurredFrom = narrowing.OccurredFrom,
            OccurredTo = narrowing.OccurredTo
        };

    static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static string[] Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
