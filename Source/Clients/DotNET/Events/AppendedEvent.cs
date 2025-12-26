// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
/// <param name="Context">The <see cref="EventContext"/>.</param>
/// <param name="Content">The content as a deserialized object.</param>
public record AppendedEvent(EventContext Context, object Content)
{
    /// <summary>
    /// Represents an empty <see cref="AppendedEvent"/> with a specific event sequence number.
    /// </summary>
    /// <param name="eventSequenceNumber">Event sequence number it should hold.</param>
    /// <returns>An empty <see cref="AppendedEvent"/> with a specific event sequence number.</returns>
    public static AppendedEvent EmptyWithEventSequenceNumber(EventSequenceNumber eventSequenceNumber) =>
        new(EventContext.Empty with { SequenceNumber = eventSequenceNumber }, new object());

    /// <summary>
    /// Represents an empty <see cref="AppendedEvent"/> with a specific event type.
    /// </summary>
    /// <param name="eventType">Type of event it should be.</param>
    /// <returns>An empty <see cref="AppendedEvent"/> with a specific event type.</returns>
    public static AppendedEvent EmptyWithEventType(EventType eventType) => new(EventContext.Empty with { EventType = eventType }, new object());

    /// <summary>
    /// Represents an empty <see cref="AppendedEvent"/> with a specific event type.
    /// </summary>
    /// <param name="content">The content for the event.</param>
    /// <returns>An empty <see cref="AppendedEvent"/> with a specific event type.</returns>
    public static AppendedEvent EmptyWithContent(object content) => new(EventContext.Empty, content);

    /// <summary>
    /// Represents an empty <see cref="AppendedEvent"/> with a specific event type.
    /// </summary>
    /// <param name="eventType">Type of event it should be.</param>
    /// <param name="eventSequenceNumber">Event sequence number it should hold.</param>///
    /// <returns>An empty <see cref="AppendedEvent"/> with a specific event type.</returns>
    public static AppendedEvent EmptyWithEventTypeAndEventSequenceNumber(EventType eventType, EventSequenceNumber eventSequenceNumber) =>
        new(EventContext.Empty with { EventType = eventType, SequenceNumber = eventSequenceNumber }, new object());
}
