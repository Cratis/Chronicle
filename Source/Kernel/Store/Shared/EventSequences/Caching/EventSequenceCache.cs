// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCache"/>.
/// </summary>
public class EventSequenceCache : IEventSequenceCache
{
    readonly SortedList<EventSequenceNumber, AppendedEvent> _events;
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId { get; }

    /// <inheritdoc/>
    public int RangeSize { get; }

    /// <inheritdoc/>
    public EventSequenceCacheRange CurrentRange { get; }

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Content
    {
        get
        {
            lock (_events)
            {
                return _events.Values;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCache"/> class.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> the cache is for.</param>
    /// <param name="rangeSize">Size of the range to keep cached.</param>
    /// <param name="eventSequenceStorageProvider"><see cref="IEventSequenceStorageProvider"/> for working with the event sequences.</param>
    public EventSequenceCache(
        EventSequenceId eventSequenceId,
        int rangeSize,
        IEventSequenceStorageProvider eventSequenceStorageProvider)
    {
        EventSequenceId = eventSequenceId;
        RangeSize = rangeSize;
        _events = new(new EventSequenceNumberComparer());
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        CurrentRange = new(0, 0);

        Task.Run(async () =>
        {
            var cursor = await _eventSequenceStorageProvider.GetRange(eventSequenceId, 0, (ulong)rangeSize);
            while (await cursor.MoveNext())
            {
                Feed(cursor.Current);
            }
        }).Wait();
    }

    /// <inheritdoc/>
    public void Feed(IEnumerable<AppendedEvent> events)
    {
        lock (_events)
        {
            foreach (var @event in events)
            {
                _events.Add(@event.Metadata.SequenceNumber, @event);
            }
        }
    }

    /// <inheritdoc/>
    public IEventCursor GetFrom(EventSequenceNumber sequenceNumber) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventCursor GetRange(EventSequenceNumber start, EventSequenceNumber end) => throw new NotImplementedException();
}
