// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using NJsonSchema;
using ContractsEventType = Cratis.Chronicle.Contracts.Events.EventType;
using ContractsEventTypeRegistration = Cratis.Chronicle.Contracts.Events.EventTypeRegistration;
using IEventTypesService = Cratis.Chronicle.Contracts.Events.IEventTypes;

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents the API for working with event types.
/// </summary>
[Route("/api/event-store/{eventStore}/types")]
public class EventTypeCommands : Controller
{
    readonly IEventTypesService _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeCommands"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypesService"/> service for actual registration.</param>
    internal EventTypeCommands(IEventTypesService eventTypes)
    {
        _eventTypes = eventTypes;
    }

    /// <summary>
    /// Register schemas.
    /// </summary>
    /// <param name="eventStore">Event store to register for.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public Task Register(
        [FromRoute] string eventStore,
        [FromBody] RegisterEventTypes payload) =>
            _eventTypes.Register(new RegisterEventTypesRequest
            {
                EventStore = eventStore,
                Types = payload.Types.ToContract().ToList()
            });

    /// <summary>
    /// Create a new event type.
    /// </summary>
    /// <param name="eventStore">Event store to create for.</param>
    /// <param name="command">Command for creating the event type.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("create")]
    public async Task CreateEventType(
        [FromRoute] string eventStore,
        [FromBody] EventTypes.CreateEventType command)
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
        };

        await _eventTypes.RegisterSingle(new RegisterSingleEventTypeRequest
        {
            EventStore = eventStore,
            Type = new ContractsEventTypeRegistration
            {
                Type = new ContractsEventType
                {
                    Id = command.Name,
                    Generation = 1
                },
                Owner = (Contracts.Events.EventTypeOwner)(int)EventTypeOwner.Client,
                Source = (Contracts.Events.EventTypeSource)(int)EventTypeSource.User,
                Schema = schema.ToJson()
            }
        });
    }
}
