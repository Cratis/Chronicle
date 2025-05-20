// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Api.Events;
using IEventTypes = Cratis.Chronicle.Contracts.Events.IEventTypes;

namespace Cratis.Chronicle.Api.EventTypes;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
[Route("/api/event-store/{eventStore}/types")]
public class EventTypeQueries : ControllerBase
{
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeQueries"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> service for actual registration.</param>
    internal EventTypeQueries(IEventTypes eventTypes)
    {
        _eventTypes = eventTypes;
    }

    /// <summary>
    /// Gets all event types.
    /// </summary>
    /// <param name="eventStore">The event store to get event types for.</param>
    /// <returns>Collection of event types.</returns>
    [HttpGet]
    public async Task<IEnumerable<EventType>> AllEventTypes([FromRoute] string eventStore) =>
        (await _eventTypes.GetAll(new() { EventStore = eventStore })).ToApi();

    /// <summary>
    /// Gets all event types with schemas.
    /// </summary>
    /// <param name="eventStore">The event store to get event types for.</param>
    /// <returns>Collection of event types with schemas.</returns>
    [HttpGet("schemas")]
    public async Task<IEnumerable<EventTypeRegistration>> AllEventTypesWithSchemas([FromRoute] string eventStore) =>
        (await _eventTypes.GetAllRegistrations(new() { EventStore = eventStore })).ToApi();
}
