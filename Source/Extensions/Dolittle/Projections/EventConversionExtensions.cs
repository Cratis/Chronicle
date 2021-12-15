// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Events;
using MongoDB.Bson.Serialization;
using Event = Cratis.Events.Projections.Event;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Extension methods for converting event types.
    /// </summary>
    public static class EventConversionExtensions
    {
        /// <summary>
        /// Convert a <see cref="EventStore.Event"/> to <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="EventStore.Event"/> to convert from.</param>
        /// <returns>Converted <see cref="Event"/>.</returns>
        public static Event ToCratis(this EventStore.Event @event)
        {
            var eventType = new EventType(@event.Metadata.TypeId, @event.Metadata.TypeGeneration);
            var content = BsonSerializer.Deserialize<ExpandoObject>(@event.Content);
            return new Event(
                @event.Id,
                eventType,
                @event.Metadata.Occurred,
                @event.Metadata.EventSource,
                content);
        }
    }
}
