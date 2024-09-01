// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Api.EventTypes;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
[Route("/api/event-store/{eventStore}/types")]
public class EventTypeQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Gets all event types.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get event types for.</param>
    /// <returns>Collection of event types.</returns>
    [HttpGet]
    public async Task<IEnumerable<EventType>> AllEventTypes([FromRoute] string eventStore)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var eventTypes = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => new EventType(_.Type.Id, _.Type.Generation)).ToArray();
    }

    /// <summary>
    /// Gets generation schema for type.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get event type for.</param>
    /// <param name="eventTypeId">Type to get for.</param>
    /// <returns>Schemas.</returns>
    [HttpGet("schemas/{eventTypeId}")]
    public async Task<IEnumerable<JsonDocument>> GenerationSchemasForType(
        [FromQuery] EventStoreName eventStore,
        [FromRoute] string eventTypeId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets all event types with schemas.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get event types for.</param>
    /// <returns>Collection of event types with schemas.</returns>
    [HttpGet("schemas")]
    public async Task<IEnumerable<EventTypeWithSchemas>> AllEventTypesWithSchemas([FromRoute] string eventStore)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var eventTypes = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => new EventTypeWithSchemas(new EventType(_.Type.Id, _.Type.Generation), [JsonDocument.Parse(_.Schema.ToJson())])).ToArray();
    }
}
