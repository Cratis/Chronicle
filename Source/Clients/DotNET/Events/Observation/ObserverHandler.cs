// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store;
using Cratis.Events.Store.Grains.Observation;
using Cratis.Events.Store.Observation;

namespace Cratis.Events.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IObserver"/>.
    /// </summary>
    public class ObserverHandler
    {
        readonly IEventTypes _eventTypes;
        readonly IObserverInvoker _observerInvoker;
        readonly IEventSerializer _eventSerializer;

        public ObserverHandler(
            ObserverId observerId,
            ObserverName name,
            EventLogId eventLogId,
            IEventTypes eventTypes,
            IObserverInvoker observerInvoker,
            IEventSerializer eventSerializer)
        {
            ObserverId = observerId;
            Name = name;
            EventLogId = eventLogId;
            _eventTypes = eventTypes;
            _observerInvoker = observerInvoker;
            _eventSerializer = eventSerializer;
        }

        public ObserverId ObserverId { get; }
        public ObserverName Name { get; }
        public EventLogId EventLogId { get; }

        public IEnumerable<EventType> EventTypes => _observerInvoker.EventTypes;

        public async Task OnNext(AppendedEvent @event)
        {
            var eventType = _eventTypes.GetClrTypeFor(@event.Metadata.EventType.Id);
            var content = _eventSerializer.Deserialize(eventType, @event.Content);
            await _observerInvoker.Invoke(content, @event.EventContext);
        }
    }
}
