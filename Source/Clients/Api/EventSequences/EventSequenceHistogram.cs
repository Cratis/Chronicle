// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Contracts.EventSequences;
using IEventSequencesService = Cratis.Chronicle.Contracts.EventSequences.IEventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents a single bucket in an event sequence histogram.
/// </summary>
/// <param name="EventSequenceNumber">The lowest event sequence number in the bucket.</param>
/// <param name="Occurred">The earliest timestamp in the bucket.</param>
/// <param name="Count">Number of events contained in the bucket.</param>
[ReadModel]
public record EventSequenceHistogram(
    ulong EventSequenceNumber,
    DateTimeOffset Occurred,
    long Count)
{
    /// <summary>
    /// Gets the histogram for a given event sequence.
    /// </summary>
    /// <param name="eventStore">The event store to read from.</param>
    /// <param name="namespace">The namespace to read from.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <param name="resolution">The histogram resolution (Minute, Hour, Day, Week, Month).</param>
    /// <param name="eventSequences">The <see cref="IEventSequencesService"/> gRPC service.</param>
    /// <returns>The ordered collection of histogram buckets.</returns>
    public static async Task<IEnumerable<EventSequenceHistogram>> ForSequence(
        string eventStore,
        string @namespace,
        string eventSequenceId,
        string resolution,
        IEventSequencesService eventSequences)
    {
        var parsedResolution = Enum.TryParse<EventSequenceHistogramResolution>(resolution, ignoreCase: true, out var value)
            ? value
            : EventSequenceHistogramResolution.Hour;

        var response = await eventSequences.GetHistogram(new GetHistogramRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            Resolution = parsedResolution switch
            {
                EventSequenceHistogramResolution.Minute => HistogramResolution.Minute,
                EventSequenceHistogramResolution.Hour => HistogramResolution.Hour,
                EventSequenceHistogramResolution.Day => HistogramResolution.Day,
                EventSequenceHistogramResolution.Week => HistogramResolution.Week,
                EventSequenceHistogramResolution.Month => HistogramResolution.Month,
                _ => HistogramResolution.Hour
            }
        });

        return response.Buckets.Select(b => new EventSequenceHistogram(
            b.EventSequenceNumber,
            DateTimeOffset.Parse(b.Occurred.Value, CultureInfo.InvariantCulture),
            b.Count));
    }
}
