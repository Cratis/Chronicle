// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Contracts.Events;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Kernel.Domain.Events;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
[Route("/api/events/store/{eventStoreName}/types")]
public class EventTypes : Controller
{
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> service for actual registration.</param>
    public EventTypes(IEventTypes eventTypes)
    {
        _eventTypes = eventTypes;
    }

    /// <summary>
    /// Register schemas.
    /// </summary>
    /// <param name="eventStoreName"><see cref="EventStoreName"/> to register for.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public Task Register(
        [FromRoute] EventStoreName eventStoreName,
        [FromBody] RegisterEventTypes payload) =>
            _eventTypes.Register(new RegisterEventTypesRequest
            {
                EventStoreName = eventStoreName,
                Types = payload.Types.ToList()
            });
}
