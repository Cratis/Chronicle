// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Defines a builder for seeding events in the event store.
/// </summary>
public interface IEventSeedingBuilder
{
    /// <summary>
    /// Seed events for a specific event type and event source id.
    /// </summary>
    /// <typeparam name="TEvent">Type of event to seed.</typeparam>
    /// <param name="eventSourceId">The event source id to seed for.</param>
    /// <param name="events">Collection of events to seed.</param>
    /// <returns>The builder for continuation.</returns>
    IEventSeedingBuilder For<TEvent>(EventSourceId eventSourceId, IEnumerable<TEvent> events)
        where TEvent : class;

    /// <summary>
    /// Seed events for a specific event source id with multiple event types.
    /// </summary>
    /// <param name="eventSourceId">The event source id to seed for.</param>
    /// <param name="events">Collection of events to seed.</param>
    /// <returns>The builder for continuation.</returns>
    IEventSeedingBuilder ForEventSource(EventSourceId eventSourceId, IEnumerable<object> events);
}
