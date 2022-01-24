// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    public class EventLog : IEventLog
    {
        readonly IEventTypes _eventTypes;
        readonly IEventSerializer _serializer;
        readonly IInstancesOf<ICanProvideAdditionalEventInformation> _additionalEventInformationProviders;
        readonly Store.Grains.IEventLog _eventLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLog"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving the types of events.</param>
        /// <param name="serializer"><see cref="IEventSerializer"/> for serializing events.</param>
        /// <param name="additionalEventInformationProviders">Providers of additional event information.</param>
        /// <param name="eventLog">The actual <see cref="Store.Grains.IEventLog"/>.</param>
        public EventLog(
            IEventTypes eventTypes,
            IEventSerializer serializer,
            IInstancesOf<ICanProvideAdditionalEventInformation> additionalEventInformationProviders,
            Store.Grains.IEventLog eventLog)
        {
            _eventTypes = eventTypes;
            _serializer = serializer;
            _additionalEventInformationProviders = additionalEventInformationProviders;
            _eventLog = eventLog;
        }

        /// <inheritdoc/>
        public async Task Append(EventSourceId eventSourceId, object @event)
        {
            var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
            var eventAsJson = _serializer.Serialize(@event!);
            foreach (var provider in _additionalEventInformationProviders)
            {
                await provider.ProvideFor(eventAsJson);
            }
            await _eventLog.Append(eventSourceId, eventType, eventAsJson);
        }
    }
}
