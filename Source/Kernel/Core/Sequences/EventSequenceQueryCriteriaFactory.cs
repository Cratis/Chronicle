// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

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
        new(
            EventSourceId: Trimmed(narrowing.EventSourceId) is { } eventSourceId ? (EventSourceId)eventSourceId : null,
            EventSourceType: Trimmed(narrowing.EventSourceType) is { } eventSourceType ? (EventSourceType)eventSourceType : null,
            EventStreamType: Trimmed(narrowing.EventStreamType) is { } eventStreamType ? (EventStreamType)eventStreamType : null,
            CorrelationId: Guid.TryParse(narrowing.CorrelationId, out var correlationId) ? (CorrelationId)correlationId : null,
            EventTypes: [.. Split(narrowing.EventTypeIds).Select(id => new Concepts.Events.EventType(id, 1))],
            Tags: [.. Split(narrowing.Tags).Select(tag => (Tag)tag)],
            OccurredFrom: narrowing.OccurredFrom,
            OccurredTo: narrowing.OccurredTo);

    /// <summary>
    /// Splits a comma separated list of event type identifiers into <see cref="Concepts.Events.EventType"/>.
    /// </summary>
    /// <param name="value">The comma separated event type identifiers, or null/blank for none.</param>
    /// <returns>The event types.</returns>
    internal static IEnumerable<Concepts.Events.EventType> SplitEventTypes(string? value) =>
        Split(value).Select(id => new Concepts.Events.EventType(id, 1));

    /// <summary>
    /// Trims a value, treating a blank value as absent.
    /// </summary>
    /// <param name="value">The value to trim.</param>
    /// <returns>The trimmed value, or null when the value was blank.</returns>
    internal static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static string[] Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
