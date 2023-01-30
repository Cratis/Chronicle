// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCache"/>.
/// </summary>
public class EventSequenceCache : IEventSequenceCache
{
    readonly object _lock = new();

    readonly SortedSet<AppendedEventWithSequenceNumber> _events;
    readonly SortedSet<AppendedEventByDate> _eventsByDate;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCache"/> class.
    /// </summary>
    public EventSequenceCache()
    {
        _events = new(new AppendedEventComparer());
        _eventsByDate = new(new AppendedEventByDateComparer());
    }

    /// <inheritdoc/>
    public void Add(AppendedEvent @event)
    {
        lock (_lock)
        {
            var eventWithSequenceNumber = new AppendedEventWithSequenceNumber { Event = @event, SequenceNumber = @event.Metadata.SequenceNumber };
            if (_events.Contains(eventWithSequenceNumber))
            {
                return;
            }

            _events.Add(eventWithSequenceNumber);
            _eventsByDate.Add(new(@event, DateTimeOffset.UtcNow));
        }
    }

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> GetView(EventSequenceNumber from, EventSequenceNumber? to = null)
    {
        lock (_lock)
        {
            var fromEvent = new AppendedEventWithSequenceNumber { SequenceNumber = from };
            var toEvent = new AppendedEventWithSequenceNumber { SequenceNumber = to ?? EventSequenceNumber.Max };
            var events = _events.GetViewBetween(fromEvent, toEvent);
            return events.Select(_ => _.Event);
        }
    }
}
