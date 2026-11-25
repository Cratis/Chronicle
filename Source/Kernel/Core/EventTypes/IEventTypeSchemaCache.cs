// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Defines a silo-wide view over the event type registry that hands out schemas in their JSON form, as carried alongside an
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
    /// Propagates whatever the event type lookup throws for a type the registry does not hold.
    /// </exception>
    string GetSchemaJsonFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation);

    /// <summary>
    /// Gets effective protection from the current definition snapshot, including its owning generation schema.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventTypeId">The event type.</param>
    /// <param name="generation">The content generation.</param>
    /// <param name="ownSchema">The caller's own generation schema. It never decides the result, which always comes from the definition snapshot.</param>
    /// <returns>The effective protection schema.</returns>
    ValueTask<JsonSchema> GetProtectionSchemaFor(EventStoreName eventStore, EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema ownSchema);
}
