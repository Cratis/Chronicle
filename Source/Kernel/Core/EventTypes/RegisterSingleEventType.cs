// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the command for registering one event type.
/// </summary>
/// <param name="EventStore">The event store to register into.</param>
/// <param name="Type">The event type to register.</param>
[Command]
[BelongsTo(WellKnownServices.EventTypes)]
public record RegisterSingleEventType(string EventStore, Contracts.Events.EventTypeRegistration Type)
{
    /// <summary>
    /// Handles the command by writing the registration and evicting the cache when it changed anything.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <param name="eventTypesCacheClient">Client for evicting the event type cache on every silo.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IStorage storage, IEventTypesCacheClient eventTypesCacheClient)
    {
        var chronicleType = Type.Type.ToChronicle();
        var schema = await JsonSchema.FromJsonAsync(Type.Schema);
        var mutated = await storage
            .GetEventStore(EventStore).EventTypes
            .Register(
                chronicleType,
                schema,
                (Concepts.Events.EventTypeOwner)(int)Type.Owner,
                (Concepts.Events.EventTypeSource)(int)Type.Source);

        if (mutated)
        {
            await eventTypesCacheClient.Invalidate(EventStore, chronicleType.Id);
        }
    }
}
