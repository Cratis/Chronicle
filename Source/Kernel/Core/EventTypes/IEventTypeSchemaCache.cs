// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Defines a per-silo cache of event type schemas in their JSON form, as carried alongside an
/// <see cref="AppendedEvent"/> when it crosses a silo boundary.
/// </summary>
public interface IEventTypeSchemaCache
{
    /// <summary>
    /// Get the JSON representation of the schema for a specific event type and generation.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the event type belongs to.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to get the schema for.</param>
    /// <param name="generation">The <see cref="EventTypeGeneration"/> to get the schema for.</param>
    /// <returns>The schema as JSON.</returns>
    /// <exception cref="Exception">
    /// Propagates whatever the underlying event type storage lookup throws. A failed lookup is not cached, so
    /// the next call retries it.
    /// </exception>
    string GetSchemaJsonFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation);

    /// <summary>
    /// Evict every cached generation of a specific <see cref="EventTypeId"/> within one event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the event type belongs to.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to evict.</param>
    /// <remarks>
    /// Scoped to <paramref name="eventStore"/> - the same <see cref="EventTypeId"/> in another event store is
    /// a distinct schema and stays cached.
    /// </remarks>
    void Invalidate(EventStoreName eventStore, EventTypeId eventTypeId);
}
