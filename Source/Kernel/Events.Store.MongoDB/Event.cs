// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

#nullable disable

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents the document representation of a stored event.
    /// </summary>
    /// <param name="SequenceNumber">The sequence number of the event - the primary key.</param>
    /// <param name="Type">The <see cref="EventTypeId">type identifier</see> of the event</param>
    /// <param name="Occurred">The time the event occurred.</param>
    /// <param name="EventSourceId">The <see cref="EventSourceId"/> for the event.</param>
    /// <param name="Content">The content per event type generation.</param>
    /// <param name="Compensations">Any compensations for the event.</param>
    public record Event(EventLogSequenceNumber SequenceNumber, EventTypeId Type, DateTimeOffset Occurred, EventSourceId EventSourceId, Dictionary<EventGeneration, BsonDocument> Content, IEnumerable<EventCompensation> Compensations);
}
