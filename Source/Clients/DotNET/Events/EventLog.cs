// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Concepts.SystemJson;

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    public class EventLog : IEventLog
    {
        readonly JsonSerializerOptions _serializerOptions;
        readonly IEventTypes _eventTypes;
        readonly Store.Grains.IEventLog _eventLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLog"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving the types of events.</param>
        /// <param name="eventLog">The actual <see cref="Store.Grains.IEventLog"/>.</param>
        public EventLog(IEventTypes eventTypes, Store.Grains.IEventLog eventLog)
        {
            _serializerOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    new ConceptAsJsonConverterFactory()
                }
            };
            _eventTypes = eventTypes;
            _eventLog = eventLog;
        }

        /// <inheritdoc/>
        public async Task Append(EventSourceId eventSourceId, object @event)
        {
            var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
            var json = JsonSerializer.Serialize(@event, _serializerOptions);
            await _eventLog.Append(eventSourceId, eventType, json);
        }
    }
}
