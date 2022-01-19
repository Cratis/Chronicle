// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents an event that has occurred.
    /// </summary>
    /// <param name="SequenceNumber">The <see cref="EventLogSequenceNumber">sequence number</see> of the event.</param>
    /// <param name="Type"><see cref="EventType">Type of event</see>.</param>
    /// <param name="Occurred">When the event occurred.</param>
    /// <param name="EventSourceId"><see cref="EventSourceId">Unique identifier for the event source</see>.</param>
    /// <param name="Content">The actual event content.</param>
    public record Event(EventLogSequenceNumber SequenceNumber, EventType Type, DateTimeOffset Occurred, EventSourceId EventSourceId, ExpandoObject Content);
}
