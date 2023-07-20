// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a handler of observers.
/// </summary>
public class ObserverHandler
{
    readonly IEventTypes _eventTypes;
    readonly IObserverInvoker _observerInvoker;
    readonly IEventSerializer _eventSerializer;

    /// <summary>
    /// Gets the unique identifier of the observer.
    /// </summary>
    public ObserverId ObserverId { get; }

    /// <summary>
    /// Gets the name of the observer.
    /// </summary>
    public ObserverName Name { get; }

    /// <summary>
    /// Gets the event sequence the observer is observing.
    /// </summary>
    public EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the event types for the observer.
    /// </summary>
    public IEnumerable<EventType> EventTypes => _observerInvoker.EventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverHandler"/>.
    /// </summary>
    /// <param name="observerId">Unique identifier.</param>
    /// <param name="name">Name of the observer.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the observer is for.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
    /// <param name="observerInvoker">The actual invoker.</param>
    /// <param name="eventSerializer">The event serializer to use.</param>
    public ObserverHandler(
        ObserverId observerId,
        ObserverName name,
        EventSequenceId eventSequenceId,
        IEventTypes eventTypes,
        IObserverInvoker observerInvoker,
        IEventSerializer eventSerializer)
    {
        ObserverId = observerId;
        Name = name;
        EventSequenceId = eventSequenceId;
        _eventTypes = eventTypes;
        _observerInvoker = observerInvoker;
        _eventSerializer = eventSerializer;
    }

    /// <summary>
    /// Handle next event.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    public async Task OnNext(AppendedEvent @event)
    {
        var eventType = _eventTypes.GetClrTypeFor(@event.Metadata.Type.Id);
        var content = await _eventSerializer.Deserialize(eventType, @event.Content);
        await _observerInvoker.Invoke(content, @event.Context);
    }
}
