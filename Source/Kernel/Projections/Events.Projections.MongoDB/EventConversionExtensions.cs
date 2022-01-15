// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson.Serialization;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Extension methods for converting event types.
    /// </summary>
    public static class EventConversionExtensions
    {
        /// <summary>
        /// Convert a <see cref="Store.MongoDB.Event"/> to <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="Store.MongoDB.Event"/> to convert from.</param>
        /// <returns>Converted <see cref="Event"/>.</returns>
        public static Event ToCratis(this Store.MongoDB.Event @event)
        {
            var eventType = new EventType(@event.Type, EventGeneration.First);
            var content = BsonSerializer.Deserialize<ExpandoObject>(@event.Content[EventGeneration.First.ToString()]);
            return new Event(
                @event.SequenceNumber,
                eventType,
                @event.Occurred,
                @event.EventSourceId,
                content);
        }
    }
}
