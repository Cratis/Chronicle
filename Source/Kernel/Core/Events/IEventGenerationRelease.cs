// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Selects and releases the generation pinned by an observer.
/// </summary>
public interface IEventGenerationRelease
{
    /// <summary>
    /// Releases events using each pinned generation's schema.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="pins">The observer's generation pins.</param>
    /// <param name="schemas">The subscribed schemas.</param>
    /// <param name="events">The events to deliver.</param>
    /// <returns>The selected and released events.</returns>
    Task<AppendedEvent[]> Release(EventStoreName eventStore, IEnumerable<EventType> pins, IDictionary<EventType, EventTypeSchema> schemas, IEnumerable<AppendedEvent> events);
}
