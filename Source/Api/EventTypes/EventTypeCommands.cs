// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypeCommands"/> class.
/// </remarks>
/// <param name="eventTypes"><see cref="IEventTypes"/> service for actual registration.</param>
[Route("/api/event-store/{eventStoreName}/types")]
public class EventTypeCommands(IEventTypes eventTypes) : Controller
{
    /// <summary>
    /// Register schemas.
    /// </summary>
    /// <param name="eventStoreName">Event store to register for.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public Task Register(
        [FromRoute] string eventStoreName,
        [FromBody] RegisterEventTypes payload) =>
            eventTypes.Register(new RegisterEventTypesRequest
            {
                EventStoreName = eventStoreName,
                Types = payload.Types.ToList()
            });
}
