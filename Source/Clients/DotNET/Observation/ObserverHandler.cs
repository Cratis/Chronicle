// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a handler of observers.
/// </summary>
public class ObserverHandler
{
    /// <summary>
    /// The causation type for client observer.
    /// </summary>
    public static readonly CausationType CausationType = new("Client Observer");

    readonly IEventTypes _eventTypes;
    readonly IObserverInvoker _observerInvoker;
    readonly ICausationManager _causationManager;
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
    /// Gets the event log for the observer.
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
    /// <param name="eventSequenceId">Event log identifier.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
    /// <param name="observerInvoker">The actual invoker.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="eventSerializer">The serializer to use.</param>
    public ObserverHandler(
        ObserverId observerId,
        ObserverName name,
        EventSequenceId eventSequenceId,
        IEventTypes eventTypes,
        IObserverInvoker observerInvoker,
        ICausationManager causationManager,
        IEventSerializer eventSerializer)
    {
        ObserverId = observerId;
        Name = name;
        EventSequenceId = eventSequenceId;
        _eventTypes = eventTypes;
        _observerInvoker = observerInvoker;
        _causationManager = causationManager;
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

        _causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { "ObserverId", ObserverId.ToString() },
            { "EventTypeId", @event.Metadata.Type.Id.ToString() },
            { "EventTypeGeneration", @event.Metadata.Type.Generation.ToString() },
            { "EventSequenceId", EventSequenceId.ToString() },
            { "EventSequenceNumber", @event.Metadata.SequenceNumber.ToString() }
        });

        // TODO: Optimize this. It shouldn't be necessary to go from Expando to Json and back to the actual type.
        var json = await _eventSerializer.Serialize(@event.Content);
        var content = await _eventSerializer.Deserialize(eventType, json);
        await _observerInvoker.Invoke(content, @event.Context);
    }
}
