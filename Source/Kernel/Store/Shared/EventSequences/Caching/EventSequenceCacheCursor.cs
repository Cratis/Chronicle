// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Represents an <see cref="IEventCursor"/> for use in conjunction with the <see cref="EventSequenceCache"/>.
/// </summary>
public class EventSequenceCacheCursor : IEventCursor
{
    readonly EventSequenceId _eventSequenceId;
    readonly IEventSequenceCache _cache;
    readonly EventSequenceNumber _end;
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;
    IEnumerable<AppendedEvent> _next;
    EventSequenceNumber _nextEventSequenceNumber = EventSequenceNumber.Unavailable;
    EventSequenceNumber _lastEventSequenceNumber = EventSequenceNumber.Unavailable;
    int _iterations;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = Array.Empty<AppendedEvent>();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCacheCursor"/> class.
    /// </summary>
    /// <param name="cache">The <see cref="IEventSequenceCache"/> that created the cursor.</param>
    /// <param name="end">The end sequence number.</param>
    /// <param name="eventsInCache">A collection of <see cref="AppendedEvent">events</see> from the cache.</param>
    /// <param name="eventSequenceStorageProvider"><see cref="IEventSequenceStorageProvider"/> for working with the event sequences.</param>
    public EventSequenceCacheCursor(
        IEventSequenceCache cache,
        EventSequenceNumber end,
        IEnumerable<AppendedEvent> eventsInCache,
        IEventSequenceStorageProvider eventSequenceStorageProvider)
    {
        _eventSequenceId = cache.EventSequenceId;
        _cache = cache;
        _end = end;
        _next = eventsInCache;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _iterations = 0;
    }

    /// <inheritdoc/>
    public async Task<bool> MoveNext()
    {
        if (_next.Any())
        {
            Current = _next;
            _next = Array.Empty<AppendedEvent>();
            _nextEventSequenceNumber = Current.Last().Metadata.SequenceNumber + 1;
            if (_nextEventSequenceNumber != EventSequenceNumber.Unavailable && _nextEventSequenceNumber < _end)
            {
                _iterations = (int)((_end - _nextEventSequenceNumber) / (ulong)_cache.RangeSize) + 1;
                _lastEventSequenceNumber = _nextEventSequenceNumber + (ulong)_cache.RangeSize;
                if (_lastEventSequenceNumber > _end)
                {
                    _lastEventSequenceNumber = _end;
                }
            }

            return true;
        }

        if (_iterations > 0)
        {
            var moveNext = false;
            var cursor = await _eventSequenceStorageProvider.GetRange(_eventSequenceId, _nextEventSequenceNumber, _lastEventSequenceNumber);
            while (await cursor.MoveNext())
            {
                _cache.Feed(cursor.Current);
                moveNext = true;
            }
            _iterations--;
            _lastEventSequenceNumber = _nextEventSequenceNumber + (ulong)_cache.RangeSize;
            if (_lastEventSequenceNumber > _end)
            {
                _lastEventSequenceNumber = _end;
            }

            return moveNext;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
