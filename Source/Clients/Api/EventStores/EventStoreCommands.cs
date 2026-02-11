// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Api.EventStores;

/// <summary>
/// Represents the API for commands related to the event store.
/// </summary>
[Route("/api/event-stores")]
public class EventStoreCommands : ControllerBase
{
    readonly IEventStores _eventStores;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreCommands"/> class.
    /// </summary>
    /// <param name="eventStores">The <see cref="IEventStores"/> contract.</param>
    internal EventStoreCommands(IEventStores eventStores)
    {
        _eventStores = eventStores;
    }

    /// <summary>
    /// Add an event store.
    /// </summary>
    /// <param name="command"><see cref="AddEventStore"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("add")]
    public Task AddEventStore([FromBody] AddEventStore command) =>
        _eventStores.Ensure(new() { Name = command.Name, Description = command.Description });
}
