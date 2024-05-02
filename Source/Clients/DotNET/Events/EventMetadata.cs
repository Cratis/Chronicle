// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events;

/// <summary>
/// Represents the metadata related to an event.
/// </summary>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/>.</param>
/// <param name="Type">The <see cref="EventType"/>.</param>
public record EventMetadata(EventSequenceNumber SequenceNumber, EventType Type)
{
    /// <summary>
    /// Represents an empty <see cref="EventMetadata"/> with a specific event sequence number.
    /// </summary>
    /// <param name="eventSequenceNumber">The event sequence number it should hold.</param>
    /// <returns>An empty <see cref="EventMetadata"/> with specified sequence number.</returns>
    public static EventMetadata EmptyWithEventSequenceNumber(EventSequenceNumber eventSequenceNumber) => new(eventSequenceNumber, EventType.Unknown);
}
