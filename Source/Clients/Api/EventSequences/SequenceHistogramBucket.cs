// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the number of events that occurred within one time bucket of an event sequence.
/// </summary>
/// <param name="Occurred">The inclusive start of the time bucket, truncated to the requested resolution.</param>
/// <param name="Count">The number of events that occurred within the bucket.</param>
[ReadModel]
public record SequenceHistogramBucket(DateTimeOffset Occurred, long Count)
{
    /// <summary>
    /// Gets the number of events per time bucket, for driving a time range picker over an event sequence.
    /// </summary>
    /// <param name="eventSequences"><see cref="IEventSequences"/> for working with event sequences.</param>
    /// <param name="eventStore">Event store to get for.</param>
    /// <param name="namespace">Namespace to get for.</param>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="resolution">The time bucket size.</param>
    /// <param name="eventSourceId">Optional event source to narrow to.</param>
    /// <param name="eventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
    /// <param name="occurredFrom">Optional inclusive lower bound on when the event occurred.</param>
    /// <param name="occurredTo">Optional exclusive upper bound on when the event occurred.</param>
    /// <returns>The buckets containing at least one matching event, ordered by time ascending.</returns>
    /// <remarks>
    /// The narrowing mirrors what the event list applies, so the picker never offers a range that
    /// produces no rows.
    /// </remarks>
    public static async Task<IEnumerable<SequenceHistogramBucket>> SequenceHistogram(
        IEventSequences eventSequences,
        string eventStore,
        string @namespace,
        string eventSequenceId,
        HistogramResolution resolution,
        string? eventSourceId = default,
        string? eventTypeIds = default,
        DateTimeOffset? occurredFrom = default,
        DateTimeOffset? occurredTo = default)
    {
        var response = await eventSequences.GetHistogram(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            Resolution = resolution,
            Criteria = EventSequenceQueryCriteriaFactory.Create(eventSourceId, eventTypeIds, occurredFrom, occurredTo)
        });

        return response.Buckets.Select(_ => new SequenceHistogramBucket(_.Occurred, _.Count)).ToArray();
    }
}
