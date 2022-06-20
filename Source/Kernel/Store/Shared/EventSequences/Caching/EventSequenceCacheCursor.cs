// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Represents an <see cref="IEventCursor"/> for use in conjunction with the <see cref="EventSequenceCache"/>.
/// </summary>
public class EventSequenceCacheCursor : IEventCursor
{
    readonly IEventSequenceCache _cache;
    readonly EventSequenceNumber _start;
    readonly EventSequenceNumber _end;
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCacheCursor"/> class.
    /// </summary>
    /// <param name="cache">The <see cref="IEventSequenceCache"/> that created the cursor.</param>
    /// <param name="start">The start sequence number.</param>
    /// <param name="end">The end sequence number.</param>
    /// <param name="eventsInCache">A collection of <see cref="AppendedEvent">events</see> from the cache.</param>
    /// <param name="eventSequenceStorageProvider"><see cref="IEventSequenceStorageProvider"/> for working with the event sequences.</param>
    public EventSequenceCacheCursor(
        IEventSequenceCache cache,
        EventSequenceNumber start,
        EventSequenceNumber end,
        IEnumerable<AppendedEvent> eventsInCache,
        IEventSequenceStorageProvider eventSequenceStorageProvider)
    {
        _cache = cache;
        _start = start;
        _end = end;
        Current = eventsInCache;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;

        Console.WriteLine(_cache);
        Console.WriteLine(_start);
        Console.WriteLine(_end);
        Console.WriteLine(_eventSequenceStorageProvider);
    }

    /// <inheritdoc/>
    public Task<bool> MoveNext()
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
