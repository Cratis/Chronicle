// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the command for creating an event type from nothing but a name.
/// </summary>
/// <param name="EventStore">The event store to create the event type in.</param>
/// <param name="Name">The name, which is also the identifier, of the event type to create.</param>
/// <remarks>
/// This is what an author does in the workbench before there is a schema to register - the event type starts as
/// an empty object at its first generation, and the schema is filled in afterwards.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventTypes)]
public record CreateEventType(EventStoreName EventStore, EventTypeId Name)
{
    /// <summary>
    /// Handles the command by registering an empty first generation for the named event type.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <param name="eventTypesCacheClient">Client for evicting the event type cache on every silo.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IStorage storage, IEventTypesCacheClient eventTypesCacheClient)
    {
        var eventType = new Concepts.Events.EventType(Name, EventTypeGeneration.First, false);
        var mutated = await storage
            .GetEventStore(EventStore).EventTypes
            .Register(
                eventType,
                new JsonSchema { Type = JsonObjectType.Object },
                EventTypeOwner.Client,
                EventTypeSource.User);

        if (mutated)
        {
            await eventTypesCacheClient.Invalidate(EventStore, eventType.Id);
        }
    }
}
