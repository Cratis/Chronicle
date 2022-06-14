// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventCursor"/> for handling events from event sequence.
/// </summary>
public class EventCursor : IEventCursor
{
    readonly EventSequenceNumber _sequenceNumber;
    readonly IEnumerable<EventType> _eventTypes;
    readonly IEventConverter _converter;
    readonly IAsyncCursor<Event>? _innerCursor;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = Array.Empty<AppendedEvent>();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventCursor"/> class.
    /// </summary>
    /// <param name="sequenceNumber">Blah.</param>
    /// <param name="eventTypes">Blasdah.</param>
    /// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
    /// <param name="innerCursor">The underlying MongoDB cursor.</param>
    public EventCursor(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType> eventTypes,
        IEventConverter converter,
        IAsyncCursor<Event>? innerCursor)
    {
        _sequenceNumber = sequenceNumber;
        _eventTypes = eventTypes;
        _converter = converter;
        _innerCursor = innerCursor;
    }

    /// <inheritdoc/>
    public async Task<bool> MoveNext()
    {
        Console.WriteLine($"MoveNext: {_sequenceNumber}:{string.Join(", ", _eventTypes.Select(_ => _.ToString()))}");
        if (_innerCursor is null) return false;
        var result = await _innerCursor.MoveNextAsync();
        if (_innerCursor.Current is not null)
        {
            Console.WriteLine($"{_sequenceNumber}:{string.Join(", ", _eventTypes.Select(_ => _.ToString()))} - {_innerCursor.Current.Count()} - {_innerCursor.Current.First().SequenceNumber}");
            Current = await Task.WhenAll(_innerCursor.Current.Select(@event => _converter.ToAppendedEvent(@event)));
        }
        else
        {
            Console.WriteLine($"MoveNext: {_sequenceNumber}:{string.Join(", ", _eventTypes.Select(_ => _.ToString()))} - nothing");
            Current = Array.Empty<AppendedEvent>();
        }
        Console.WriteLine($"MoveNext: {_sequenceNumber}:{string.Join(", ", _eventTypes.Select(_ => _.ToString()))} - result: {result}");
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _innerCursor?.Dispose();
    }
}
