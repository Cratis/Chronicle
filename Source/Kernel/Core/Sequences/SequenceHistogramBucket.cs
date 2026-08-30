// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the number of events that occurred within one time bucket of an event sequence.
/// </summary>
/// <param name="From">The inclusive start of the time bucket.</param>
/// <param name="To">The exclusive end of the time bucket.</param>
/// <param name="Count">The number of events that occurred within the bucket.</param>
/// <remarks>
/// Both bounds are carried explicitly because buckets with no events are omitted - a consumer
/// cannot infer where a bucket ends from where the next one begins.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record SequenceHistogramBucket(DateTimeOffset From, DateTimeOffset To, long Count)
{
    /// <summary>
    /// Gets the number of events per time bucket, for driving a time range picker over an event sequence.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="resolution">The time bucket size - minute, hour, day, week or month. Defaults to hour.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventSourceType">Optional event source type to narrow to.</param>
    /// <param name="eventStreamType">Optional event stream type to narrow to.</param>
    /// <param name="correlationId">Optional correlation identifier to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="tags">Optional comma separated tags to narrow to - an event matches when it carries any of them.</param>
    /// <param name="occurredFrom">Optional inclusive lower bound on when the event occurred.</param>
    /// <param name="occurredTo">Optional exclusive upper bound on when the event occurred.</param>
    /// <returns>The buckets containing at least one matching event, ordered by time ascending.</returns>
    /// <remarks>
    /// The narrowing mirrors what the event list applies, so the picker never offers a range that
    /// produces no rows.
    /// </remarks>
    public static async Task<IEnumerable<SequenceHistogramBucket>> SequenceHistogram(
        IStorage storage,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        string? resolution = default,
        string? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamType = default,
        string? correlationId = default,
        string? eventTypeIds = default,
        string? tags = default,
        DateTimeOffset? occurredFrom = default,
        DateTimeOffset? occurredTo = default)
    {
        var histogramResolution = ParseResolution(resolution);
        var criteria = EventSequenceQueryCriteriaFactory.Create(new(
            eventSourceId,
            eventSourceType,
            eventStreamType,
            correlationId,
            eventTypeIds,
            tags,
            occurredFrom,
            occurredTo));

        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId);
        var buckets = await eventSequence.GetHistogram(histogramResolution, criteria);

        return buckets
            .Select(_ => new SequenceHistogramBucket(_.Occurred, EndOf(_.Occurred, histogramResolution), _.Count))
            .ToArray();
    }

    /// <summary>
    /// Parse a resolution name, falling back to hourly buckets for anything unrecognized.
    /// </summary>
    /// <param name="resolution">The resolution name, case insensitive.</param>
    /// <returns>The <see cref="HistogramResolution"/>.</returns>
    internal static HistogramResolution ParseResolution(string? resolution) =>
        Enum.TryParse<HistogramResolution>(resolution, ignoreCase: true, out var parsed)
            ? parsed
            : HistogramResolution.Hour;

    /// <summary>
    /// Work out where a bucket ends, given where it starts.
    /// </summary>
    /// <param name="start">The inclusive start of the bucket.</param>
    /// <param name="resolution">The bucket size.</param>
    /// <returns>The exclusive end of the bucket.</returns>
    internal static DateTimeOffset EndOf(DateTimeOffset start, HistogramResolution resolution) => resolution switch
    {
        HistogramResolution.Minute => start.AddMinutes(1),
        HistogramResolution.Hour => start.AddHours(1),
        HistogramResolution.Day => start.AddDays(1),
        HistogramResolution.Week => start.AddDays(7),

        // Months vary in length, so step by calendar month rather than by a fixed span.
        HistogramResolution.Month => start.AddMonths(1),
        _ => start.AddHours(1)
    };
}
