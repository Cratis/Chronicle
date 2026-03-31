// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to adorn event types that originate from a specific event store.
/// </summary>
/// <remarks>
/// When an observer (Reactor, Reducer, or Projection) handles event types annotated with this attribute,
/// it will automatically subscribe to the inbox event sequence for the specified event store.
/// All event types handled by an observer must originate from the same event store.
/// </remarks>
/// <param name="eventStore">The name of the event store this event type originates from.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventStoreAttribute(string eventStore) : Attribute
{
    /// <summary>
    /// Gets the name of the event store this event type originates from.
    /// </summary>
    public string EventStore { get; } = eventStore;
}
