// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Api.EventTypes;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
/// <param name="eventTypes"><see cref="IEventTypes"/> for working with event types.</param>
[Route("/api/event-store/{eventStore}/types")]
public class EventTypeQueries(IEventTypes eventTypes) : ControllerBase
{
    /// <summary>
    /// Gets all event types.
    /// </summary>
    /// <param name="eventStore">The event store to get event types for.</param>
    /// <returns>Collection of event types.</returns>
    [HttpGet]
    public Task<IEnumerable<EventType>> AllEventTypes([FromRoute] string eventStore) =>
        eventTypes.GetAll(new() { EventStoreName = eventStore });

    /// <summary>
    /// Gets all event types with schemas.
    /// </summary>
    /// <param name="eventStore">The event store to get event types for.</param>
    /// <returns>Collection of event types with schemas.</returns>
    [HttpGet("schemas")]
    public Task<IEnumerable<EventTypeRegistration>> AllEventTypesWithSchemas([FromRoute] string eventStore) =>
        eventTypes.GetAllRegistrations(new() { EventStoreName = eventStore });
}
