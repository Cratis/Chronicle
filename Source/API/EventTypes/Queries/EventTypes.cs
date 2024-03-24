// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Events;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.EventTypes.Queries;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypes"/> class.
/// </remarks>
[Route("/api/events/store/{eventStore}/types")]
public class EventTypes() : ControllerBase
{
    /// <summary>
    /// Gets all event types.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get event types for.</param>
    /// <returns>Collection of event types.</returns>
    [HttpGet]
    public async Task<IEnumerable<EventType>> AllEventTypes([FromRoute] EventStoreName eventStore)
    {
        throw new NotImplementedException();
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
        [FromRoute] EventTypeId eventTypeId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets all event types with schemas.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get event types for.</param>
    /// <returns>Collection of event types with schemas.</returns>
    [HttpGet("schemas")]
    public async Task<IEnumerable<EventTypeWithSchemas>> AllEventTypesWithSchemas([FromRoute] EventStoreName eventStore)
    {
        throw new NotImplementedException();
    }
}
