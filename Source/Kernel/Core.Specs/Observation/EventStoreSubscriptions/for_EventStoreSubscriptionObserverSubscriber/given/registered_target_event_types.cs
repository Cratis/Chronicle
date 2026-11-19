// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.given;

public static class registered_target_event_types
{
    public static async Task Register(IStorage storage, params EventTypeId[] ids)
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"message":{"type":"string"}}}""");
        var types = storage.GetEventStore("Lobby").EventTypes;
        foreach (var id in ids)
        {
            types.HasFor(id).Returns(true);
            types.HasFor(id, EventTypeGeneration.First).Returns(true);
            types.GetDefinition(id).Returns(new EventTypeDefinition(id, EventTypeOwner.Client, false, [new EventTypeGenerationDefinition(EventTypeGeneration.First, schema)], []));
            var registered = new EventTypeSchema(new(id, EventTypeGeneration.First), EventTypeOwner.Client, EventTypeSource.Code, schema);
            types.GetFor(id, EventTypeGeneration.First).Returns(registered);
            types.GetAllGenerationsForEventType(Arg.Is<EventType>(_ => _.Id == id)).Returns([registered]);
        }
    }
}
