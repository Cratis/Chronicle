// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Defines a scoped builder for seeding events with namespace targeting.
/// </summary>
public interface IEventSeedingScopeBuilder
{
    /// <summary>
    /// Seed events for a specific event type and event source id.
    /// </summary>
    /// <typeparam name="TEvent">Type of event to seed.</typeparam>
    /// <param name="eventSourceId">The event source id to seed for.</param>
    /// <param name="events">Collection of events to seed.</param>
    /// <returns>The builder for continuation.</returns>
    IEventSeedingScopeBuilder For<TEvent>(EventSourceId eventSourceId, IEnumerable<TEvent> events)
        where TEvent : class;

    /// <summary>
    /// Seed events for a specific event source id with multiple event types.
    /// </summary>
    /// <param name="eventSourceId">The event source id to seed for.</param>
    /// <param name="events">Collection of events to seed.</param>
    /// <returns>The builder for continuation.</returns>
    IEventSeedingScopeBuilder ForEventSource(EventSourceId eventSourceId, IEnumerable<object> events);

    /// <summary>
    /// Seeds events of one type onto an event stream in this namespace.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventStreamType">The stream type.</param>
    /// <param name="eventStreamId">The stream identifier.</param>
    /// <param name="events">The events to seed.</param>
    /// <param name="eventSourceType">The optional source type.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventSeedingRoutingNotSupported">The implementation does not support routing.</exception>
    IEventSeedingScopeBuilder For<TEvent>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, IEnumerable<TEvent> events, EventSourceType? eventSourceType = default)
        where TEvent : class => throw new EventSeedingRoutingNotSupported();

    /// <summary>
    /// Seeds events onto an event stream in this namespace.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventStreamType">The stream type.</param>
    /// <param name="eventStreamId">The stream identifier.</param>
    /// <param name="events">The events to seed.</param>
    /// <param name="eventSourceType">The optional source type.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventSeedingRoutingNotSupported">The implementation does not support routing.</exception>
    IEventSeedingScopeBuilder ForEventSource(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, IEnumerable<object> events, EventSourceType? eventSourceType = default)
        => throw new EventSeedingRoutingNotSupported();

    /// <summary>
    /// Seeds individually routed events in this namespace, merging tags with event-type tags.
    /// </summary>
    /// <param name="events">The routed events.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventSeedingRoutingNotSupported">The implementation does not support routing.</exception>
    IEventSeedingScopeBuilder ForEvents(IEnumerable<EventForEventSourceId> events) => throw new EventSeedingRoutingNotSupported();
}
