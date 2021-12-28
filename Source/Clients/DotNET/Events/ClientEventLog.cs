// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Concepts.SystemJson;
using Cratis.Events.Store.Grains;

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IClientEventLog"/>.
    /// </summary>
    public class ClientEventLog : IClientEventLog
    {
        readonly JsonSerializerOptions _serializerOptions;
        readonly IEventTypes _eventTypes;
        readonly IEventLog _eventLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEventLog"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving the types of events.</param>
        /// <param name="eventLog">The actual <see cref="IEventLog"/>.</param>
        public ClientEventLog(IEventTypes eventTypes, IEventLog eventLog)
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
