// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Json;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for splitting deliveries into pages within <see cref="PageBounds"/>.
/// </summary>
public static class PageBoundsSplitting
{
    /// <summary>
    /// Gets the size an event counts for against <see cref="PageBounds.MaxBytes"/>: the UTF-8 length of its JSON content.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to measure.</param>
    /// <returns>The size in bytes.</returns>
    public static int SizeOf(AppendedEvent @event) =>
        JsonSerializer.SerializeToUtf8Bytes(@event.Content, Globals.JsonSerializerOptions).Length;

    /// <summary>
    /// Splits a page spanning partitions into pages within the bounds, keeping the order of the events.
    /// </summary>
    /// <param name="bounds">The <see cref="PageBounds"/> to stay within.</param>
    /// <param name="batches">The <see cref="ObserverPartitionBatch"/> of each partition in the page.</param>
    /// <returns>The pages, in order. A partition's events can span consecutive pages.</returns>
    public static IReadOnlyList<IReadOnlyList<ObserverPartitionBatch>> Split(this PageBounds bounds, IReadOnlyList<ObserverPartitionBatch> batches)
    {
        var pages = new List<IReadOnlyList<ObserverPartitionBatch>>();
        var page = new List<ObserverPartitionBatch>();
        var events = new List<AppendedEvent>();
        var counter = new PageCounter(bounds);
        foreach (var batch in batches)
        {
            foreach (var @event in batch.Events)
            {
                if (!counter.Admit(@event))
                {
                    CloseBatch(page, batch, events);
                    pages.Add(page);
                    page = [];
                    counter.StartOver();
                }

                events.Add(@event);
            }

            CloseBatch(page, batch, events);
        }

        if (page.Count > 0)
        {
            pages.Add(page);
        }

        return pages;
    }

    /// <summary>
    /// Splits the events of one partition into pages within the bounds, keeping their order.
    /// </summary>
    /// <param name="bounds">The <see cref="PageBounds"/> to stay within.</param>
    /// <param name="events">The events of the partition, in sequence order.</param>
    /// <returns>The pages of events, in order.</returns>
    public static IReadOnlyList<IReadOnlyList<AppendedEvent>> Split(this PageBounds bounds, IReadOnlyList<AppendedEvent> events)
    {
        var pages = new List<IReadOnlyList<AppendedEvent>>();
        var page = new List<AppendedEvent>();
        var counter = new PageCounter(bounds);
        foreach (var @event in events)
        {
            if (!counter.Admit(@event))
            {
                pages.Add(page);
                page = [];
                counter.StartOver();
            }

            page.Add(@event);
        }

        if (page.Count > 0)
        {
            pages.Add(page);
        }

        return pages;
    }

    static void CloseBatch(List<ObserverPartitionBatch> page, ObserverPartitionBatch batch, List<AppendedEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        page.Add(batch with { Events = [.. events] });
        events.Clear();
    }

    /// <summary>
    /// Counts what a page holds, and admits the first event of an empty page whatever its size, so nothing ever stalls.
    /// </summary>
    /// <param name="bounds">The <see cref="PageBounds"/> to count against.</param>
    sealed class PageCounter(PageBounds bounds)
    {
        int _events;
        long _bytes;
        long _refused;

        /// <summary>
        /// Admits an event to the current page when it fits.
        /// </summary>
        /// <param name="event">The event to admit.</param>
        /// <returns>True when it fits; false when the page is full and the event starts the next one through <see cref="StartOver"/>.</returns>
        public bool Admit(AppendedEvent @event)
        {
            var size = bounds.MeasuresBytes ? SizeOf(@event) : 0;
            if (_events > 0 && (_events + 1 > bounds.MaxEvents || _bytes + size > bounds.MaxBytes))
            {
                _refused = size;
                return false;
            }

            _events++;
            _bytes += size;
            return true;
        }

        /// <summary>
        /// Starts the next page with the event that did not fit the last.
        /// </summary>
        public void StartOver()
        {
            _events = 1;
            _bytes = _refused;
        }
    }
}
