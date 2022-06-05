// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Exception that gets thrown when an event types needs to be marked public and it is not.
/// </summary>
public class EventTypeNeedsToBeMarkedPublic : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeNeedsToBeMarkedPublic"/> class.
    /// </summary>
    /// <param name="type">Type of event type.</param>
    public EventTypeNeedsToBeMarkedPublic(Type type) : base($"EventType '{type.FullName}' needs to be marked as public before it can be appended to the outbox. Use the `isPublic: true` as parameter for the attribute.")
    {
    }
}
