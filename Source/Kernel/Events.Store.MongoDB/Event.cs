// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

#nullable disable

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents the document representation of a stored event.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Gets the sequence number of the event - the primary key
        /// </summary>
        public uint SequenceNumber { get; init; }

        /// <summary>
        /// Gets the <see cref="EventTypeId">type identifier</see> of the event
        /// </summary>
        public EventTypeId Type { get; init; }

        /// <summary>
        /// Gets the time the event occurred.
        /// </summary>
        public DateTimeOffset Occurred { get; init; }

        /// <summary>
        /// Gets the <see cref="EventSourceId"/> for the event.
        /// </summary>
        public EventSourceId EventSourceId { get; init; }

        /// <summary>
        /// Gets the content per event type generation.
        /// </summary>
        public Dictionary<string, BsonDocument> Content { get; init; } = new();
    }
}
