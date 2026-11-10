// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Delivers events to a client observer in pages within its <see cref="PageBounds"/>, one page after another, and settles
/// each partition as if it had been delivered in one call.
/// </summary>
/// <remarks>
/// A partition whose events span several pages stops at the first page it is not handled in: its later events are not
/// delivered, and its result is that page's result. Events of the partition handled in earlier pages stay handled, so a
/// failed or deferred result that reports nothing handled is completed with the last event handled before it.
/// </remarks>
public static class BoundedPageDelivery
{
    /// <summary>
    /// Delivers a page spanning partitions within the bounds.
    /// </summary>
    /// <param name="bounds">The <see cref="PageBounds"/> to stay within.</param>
    /// <param name="batches">The <see cref="ObserverPartitionBatch"/> of each partition in the page.</param>
    /// <param name="deliver">The delivery of one page within the bounds.</param>
    /// <param name="stateSize">Optional size of the state each partition is delivered with, which counts toward the byte bound.</param>
    /// <returns>The <see cref="ObserverPageResult"/> with one result per partition.</returns>
    public static async Task<ObserverPageResult> Deliver(
        PageBounds bounds,
        IReadOnlyList<ObserverPartitionBatch> batches,
        Func<IReadOnlyList<ObserverPartitionBatch>, Task<ObserverPageResult>> deliver,
        Func<Key, long>? stateSize = null)
    {
        var pages = bounds.IsBounded ? bounds.Split(batches, stateSize) : [batches];
        if (pages.Count <= 1)
        {
            return await deliver(batches);
        }

        var results = new Dictionary<Key, ObserverSubscriberResult>();
        foreach (var page in pages)
        {
            var remaining = page.Where(batch => !results.TryGetValue(batch.Partition, out var earlier) || earlier.State == ObserverSubscriberState.Ok).ToArray();
            if (remaining.Length == 0)
            {
                continue;
            }

            var pageResult = await deliver(remaining);
            foreach (var batch in remaining)
            {
                var result = pageResult.Results.TryGetValue(batch.Partition, out var answered)
                    ? answered
                    : ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "The subscriber did not answer for the partition");
                results[batch.Partition] = results.TryGetValue(batch.Partition, out var earlier) ? Following(earlier, result) : result;
            }
        }

        return new(results);
    }

    /// <summary>
    /// Delivers the events of one partition within the bounds.
    /// </summary>
    /// <param name="bounds">The <see cref="PageBounds"/> to stay within.</param>
    /// <param name="events">The events of the partition, in sequence order.</param>
    /// <param name="deliver">The delivery of one page of the partition's events within the bounds.</param>
    /// <param name="stateSize">The size of the state the partition is delivered with, counted toward the byte bound of every page; zero when there is none.</param>
    /// <returns>The <see cref="ObserverSubscriberResult"/> for the partition.</returns>
    public static async Task<ObserverSubscriberResult> Deliver(
        PageBounds bounds,
        IEnumerable<AppendedEvent> events,
        Func<IEnumerable<AppendedEvent>, Task<ObserverSubscriberResult>> deliver,
        long stateSize = 0)
    {
        if (!bounds.IsBounded)
        {
            return await deliver(events);
        }

        var pages = bounds.Split(events as IReadOnlyList<AppendedEvent> ?? [.. events], stateSize);
        if (pages.Count <= 1)
        {
            return await deliver(events);
        }

        ObserverSubscriberResult? result = null;
        foreach (var page in pages)
        {
            var pageResult = await deliver(page);
            result = result is null ? pageResult : Following(result, pageResult);
            if (pageResult.State != ObserverSubscriberState.Ok)
            {
                break;
            }
        }

        return result!;
    }

    static ObserverSubscriberResult Following(ObserverSubscriberResult earlier, ObserverSubscriberResult later) =>
        later.HandledAnyEvents || !earlier.HandledAnyEvents
            ? later
            : later with { LastSuccessfulObservation = earlier.LastSuccessfulObservation };
}
