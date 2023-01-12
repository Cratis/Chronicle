// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Events;
using Aksio.Cratis.Schemas;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Domain.Projections;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
[Route("/api/events/store/{microserviceId}/types")]
public class EventTypes : Controller
{
    readonly ProviderFor<Aksio.Cratis.Schemas.ISchemaStore> _schemaStoreProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="schemaStoreProvider">Underlying <see cref="Aksio.Cratis.Schemas.ISchemaStore"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public EventTypes(
        ProviderFor<Aksio.Cratis.Schemas.ISchemaStore> schemaStoreProvider,
        IExecutionContextManager executionContextManager)
    {
        _schemaStoreProvider = schemaStoreProvider;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Register schemas.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to register for.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Register(
        [FromRoute] MicroserviceId microserviceId,
        [FromBody] RegisterEventTypes payload)
    {
        _executionContextManager.Establish(microserviceId);
        foreach (var eventType in payload.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema.ToJsonString());
            await _schemaStoreProvider().Register(eventType.Type, eventType.FriendlyName, schema);
        }
    }

    /// <summary>
    /// Gets all event types.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> to get event types for.</param>
    /// <returns>Collection of event types.</returns>
    [HttpGet]
    public async Task<IEnumerable<EventTypeInformation>> AllEventTypes([FromRoute] MicroserviceId microserviceId)
    {
        _executionContextManager.Establish(microserviceId);

        var schemas = await _schemaStoreProvider().GetLatestForAllEventTypes();

        return schemas.Select(_ =>
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
        _executionContextManager.Establish(microserviceId);
        var schemas = await _schemaStoreProvider().GetAllGenerationsForEventType(new(eventTypeId, 1));
        return schemas.Select(_ => JsonDocument.Parse(_.Schema.ToJson()));
    }
}
