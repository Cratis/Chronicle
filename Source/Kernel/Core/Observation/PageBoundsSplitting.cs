// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
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
    /// <remarks>
    /// The content is serialized into a stream that only counts what is written, so measuring allocates no copy of it.
    /// </remarks>
    public static int SizeOf(AppendedEvent @event)
    {
        using var counter = new ByteCountingStream();
        JsonSerializer.Serialize(counter, @event.Content, Globals.JsonSerializerOptions);
        return (int)Math.Min(counter.Length, int.MaxValue);
    }

    /// <summary>
    /// Splits a page spanning partitions into pages within the bounds, keeping the order of the events.
    /// </summary>
    /// <param name="bounds">The <see cref="PageBounds"/> to stay within.</param>
    /// <param name="batches">The <see cref="ObserverPartitionBatch"/> of each partition in the page.</param>
    /// <param name="stateSize">
    /// Optional size of the state each partition is delivered with, which counts toward <see cref="PageBounds.MaxBytes"/> once in
    /// every page the partition is in; a reducer's page holds the state of each partition it reduces.
    /// </param>
    /// <returns>The pages, in order. A partition's events can span consecutive pages.</returns>
    public static IReadOnlyList<IReadOnlyList<ObserverPartitionBatch>> Split(this PageBounds bounds, IReadOnlyList<ObserverPartitionBatch> batches, Func<Key, long>? stateSize = null)
    {
        var pages = new List<IReadOnlyList<ObserverPartitionBatch>>();
        var page = new List<ObserverPartitionBatch>();
        var events = new List<AppendedEvent>();
        var counter = new PageCounter(bounds);
        foreach (var batch in batches)
        {
            var state = StateCost(bounds, stateSize, batch.Partition);
            var inPage = false;
            foreach (var @event in batch.Events)
            {
                var size = EventCost(bounds, @event);
                if (!counter.Admit(inPage ? size : size + state))
                {
                    CloseBatch(page, batch, events);
                    pages.Add(page);
                    page = [];
                    counter.StartOver(size + state);
                }

                inPage = true;
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
    /// <param name="stateSize">The size of the state the partition is delivered with, counted once in every page; zero when there is none.</param>
    /// <returns>The pages of events, in order.</returns>
    public static IReadOnlyList<IReadOnlyList<AppendedEvent>> Split(this PageBounds bounds, IReadOnlyList<AppendedEvent> events, long stateSize = 0)
    {
        var pages = new List<IReadOnlyList<AppendedEvent>>();
        var page = new List<AppendedEvent>();
        var counter = new PageCounter(bounds);
        var state = bounds.MeasuresBytes ? stateSize : 0;
        foreach (var @event in events)
        {
            var size = EventCost(bounds, @event);
            if (!counter.Admit(page.Count == 0 ? size + state : size))
            {
                pages.Add(page);
                page = [];
                counter.StartOver(size + state);
            }

            page.Add(@event);
        }

        if (page.Count > 0)
        {
            pages.Add(page);
        }

        return pages;
    }

    static long EventCost(PageBounds bounds, AppendedEvent @event) => bounds.MeasuresBytes ? SizeOf(@event) : 0;

    static long StateCost(PageBounds bounds, Func<Key, long>? stateSize, Key partition) =>
        bounds.MeasuresBytes && stateSize is not null ? stateSize(partition) : 0;

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

        /// <summary>
        /// Admits an event to the current page when it fits.
        /// </summary>
        /// <param name="cost">The bytes the event adds to the page, including any state it brings into the page.</param>
        /// <returns>True when it fits; false when the page is full and the event starts the next one through <see cref="StartOver"/>.</returns>
        public bool Admit(long cost)
        {
            if (_events > 0 && (_events + 1 > bounds.MaxEvents || _bytes + cost > bounds.MaxBytes))
            {
                return false;
            }

            _events++;
            _bytes += cost;
            return true;
        }

        /// <summary>
        /// Starts the next page with the event that did not fit the last.
        /// </summary>
        /// <param name="cost">The bytes the event brings into the new page, including the state of its partition.</param>
        public void StartOver(long cost)
        {
            _events = 1;
            _bytes = cost;
        }
    }

    /// <summary>
    /// A write-only stream that counts the bytes written to it and keeps none of them.
    /// </summary>
    sealed class ByteCountingStream : Stream
    {
        long _length;

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => _length;

        /// <inheritdoc/>
        public override long Position
        {
            get => _length;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => _length += count;

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer) => _length += buffer.Length;
    }
}
