// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Domain.Projections;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
[Route("/api/events/store/{microserviceId}/types")]
public class EventTypes : ControllerBase
{
    readonly ProviderFor<IEventTypesStorage> _eventTypesStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="eventTypesStorageProvider">Underlying <see cref="IEventTypesStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public EventTypes(
        ProviderFor<IEventTypesStorage> eventTypesStorageProvider,
        IExecutionContextManager executionContextManager)
    {
        _eventTypesStorageProvider = eventTypesStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Register schemas.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to register for.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task Register(
        [FromRoute] MicroserviceId microserviceId,
        [FromBody] RegisterEventTypes payload)
    {
        _executionContextManager.Establish(microserviceId);
        foreach (var eventType in payload.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema.ToJsonString());
            await _eventTypesStorageProvider().Register(eventType.Type, eventType.FriendlyName, schema);
        }
    }
}
