// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Domain.Projections;

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
    /// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
    public EventTypes(IStorage storage)
    {
        _storage = storage;
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
        foreach (var eventType in payload.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema.ToJsonString());
            await _storage.GetEventStore((string)microserviceId).EventTypes.Register(eventType.Type, eventType.FriendlyName, schema);
        }
    }
}
