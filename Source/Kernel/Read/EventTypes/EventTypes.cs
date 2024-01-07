// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventTypes;
using Aksio.Cratis.Kernel.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.EventTypes;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
[Route("/api/events/store/{microserviceId}/types")]
public class EventTypes : ControllerBase
{
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
    public EventTypes(IStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Gets all event types.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> to get event types for.</param>
    /// <returns>Collection of event types.</returns>
    [HttpGet]
    public async Task<IEnumerable<EventTypeInformation>> AllEventTypes([FromRoute] MicroserviceId microserviceId)
    {
        var eventTypes = await _storage.GetEventStore((string)microserviceId).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ =>
            new EventTypeInformation(
                _.Type.Id.ToString(),
                _.Schema.GetDisplayName(),
                _.Schema.GetGeneration()));
    }

    /// <summary>
    /// Gets generation schema for type.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> to get event type for.</param>
    /// <param name="eventTypeId">Type to get for.</param>
    /// <returns>Schemas.</returns>
    [HttpGet("schemas/{eventTypeId}")]
    public async Task<IEnumerable<JsonDocument>> GenerationSchemasForType(
        [FromQuery] MicroserviceId microserviceId,
        [FromRoute] EventTypeId eventTypeId)
    {
        var schemas = await _storage.GetEventStore((string)microserviceId).EventTypes.GetAllGenerationsForEventType(new(eventTypeId, 1));
        return schemas.Select(_ => JsonDocument.Parse(_.Schema.ToJson()));
    }

    /// <summary>
    /// Gets all event types with schemas.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> to get event types for.</param>
    /// <returns>Collection of event types with schemas.</returns>
    [HttpGet("schemas")]
    public async Task<IEnumerable<EventTypeWithSchemas>> AllEventTypesWithSchemas([FromRoute] MicroserviceId microserviceId)
    {
        var schemas = await _storage.GetEventStore((string)microserviceId).EventTypes.GetLatestForAllEventTypes();

        return schemas.Select(_ =>
            new EventTypeWithSchemas(
                new EventTypeInformation(
                    _.Type.Id.ToString(),
                    _.Schema.GetDisplayName(),
                    _.Schema.GetGeneration()),
                new[] { JsonDocument.Parse(_.Schema.ToJson()) }));
    }
}
