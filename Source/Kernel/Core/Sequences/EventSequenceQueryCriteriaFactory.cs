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
    /// Create criteria with required named tag narrowing. Unlike the legacy query, an empty or invalid
    /// named criterion must not silently turn into an unfiltered read.
    /// </summary>
    /// <param name="narrowing">The existing query dimensions.</param>
    /// <param name="namedTags">One or more named tag criteria for gRPC callers.</param>
    /// <returns>The combined criteria.</returns>
    /// <exception cref="InvalidNamedTagCriterion">No valid named tag criteria were supplied.</exception>
    public static EventSequenceQueryCriteria CreateWithNamedTags(
        EventSequenceQueryNarrowing narrowing,
        IEnumerable<NamedTagQueryCriterion>? namedTags)
    {
        if (namedTags is null)
        {
            throw new InvalidNamedTagCriterion();
        }

        var snapshot = namedTags.ToArray();
        if (snapshot.Length == 0 || snapshot.Any(tag => tag is null || string.IsNullOrWhiteSpace(tag.Name) ||
            (tag.Values is null ? !tag.AnyValue : tag.AnyValue || !tag.Values.Any())))
        {
            throw new InvalidNamedTagCriterion();
        }

        return Create(narrowing) with
        {
            NamedTags = snapshot.Select(tag => new NamedTagCriterion((TagName)tag.Name, tag.Values)).ToArray()
        };
    }

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
