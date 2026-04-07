// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to adorn event types or an assembly to indicate that event types originate from a specific event store.
/// </summary>
/// <remarks>
/// When applied to an event type class, it identifies the event store the event originates from.
/// When applied at the assembly level, all event types in that assembly are considered to originate from the specified event store.
/// A type-level attribute takes precedence over an assembly-level attribute.
/// When an observer (Reactor, Reducer, or Projection) handles event types annotated with this attribute,
/// it will automatically subscribe to the inbox event sequence for the specified event store.
/// All event types handled by an observer must originate from the same event store.
/// </remarks>
/// <param name="eventStore">The name of the event store this event type originates from.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class EventStoreAttribute(string eventStore) : Attribute
{
    /// <summary>
    /// Gets the name of the event store this event type originates from.
    /// </summary>
    public string EventStore { get; } = eventStore;
}
