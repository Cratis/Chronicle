// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
/// <param name="Metadata">The <see cref="EventMetadata"/>.</param>
/// <param name="Context">The <see cref="EventContext"/>.</param>
/// <param name="Content">The content in the form of an <see cref="ExpandoObject"/>.</param>
public record AppendedEvent(EventMetadata Metadata, EventContext Context, ExpandoObject Content)
{
    /// <summary>
    /// Represents an empty <see cref="AppendedEvent"/> with a specific event sequence number.
    /// </summary>
    /// <param name="eventSequenceNumber">Event sequence number it should hold.</param>
    /// <returns>An empty <see cref="AppendedEvent"/> with a specific event sequence number.</returns>
    public static AppendedEvent EmptyWithEventSequenceNumber(EventSequenceNumber eventSequenceNumber) => new(EventMetadata.EmptyWithEventSequenceNumber(eventSequenceNumber), EventContext.Empty, new ExpandoObject());

    /// <summary>
    /// Represents an empty <see cref="AppendedEvent"/> with a specific event type.
    /// </summary>
    /// <param name="eventType">Type of event it should be.</param>
    /// <returns>An empty <see cref="AppendedEvent"/> with a specific event type.</returns>
    public static AppendedEvent EmptyWithEventType(EventType eventType) => new(new EventMetadata(EventSequenceNumber.First, eventType), EventContext.Empty, new ExpandoObject());

    /// <summary>
    /// Represents an empty <see cref="AppendedEvent"/> with a specific event type.
    /// </summary>
    /// <param name="eventType">Type of event it should be.</param>
    /// <param name="eventSequenceNumber">Event sequence number it should hold.</param>///
    /// <returns>An empty <see cref="AppendedEvent"/> with a specific event type.</returns>
    public static AppendedEvent EmptyWithEventTypeAndEventSequenceNumber(EventType eventType, EventSequenceNumber eventSequenceNumber) => new(new EventMetadata(eventSequenceNumber, eventType), EventContext.Empty, new ExpandoObject());
}
