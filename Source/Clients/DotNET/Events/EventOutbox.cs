// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventOutbox"/>.
/// </summary>
public class EventOutbox : IEventOutbox
{
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _serializer;
    readonly Store.Grains.IEventSequence _eventOutbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLog"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving the types of events.</param>
    /// <param name="serializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="eventOutbox">The actual <see cref="Store.Grains.IEventSequence"/>.</param>
    public EventOutbox(
        IEventTypes eventTypes,
        IEventSerializer serializer,
        Store.Grains.IEventSequence eventOutbox)
    {
        _eventTypes = eventTypes;
        _serializer = serializer;
        _eventOutbox = eventOutbox;
    }

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event)
    {
        var type = @event.GetType();
        var eventType = _eventTypes.GetEventTypeFor(type);
        if (!eventType.IsPublic)
        {
            throw new EventTypeNeedsToBeMarkedPublic(type);
        }

        var eventAsJson = await _serializer.Serialize(@event!);
        await _eventOutbox.Append(eventSourceId, eventType, eventAsJson);
    }
}
