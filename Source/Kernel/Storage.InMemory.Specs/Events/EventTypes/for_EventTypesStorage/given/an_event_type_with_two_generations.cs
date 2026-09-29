// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.given;

public class an_event_type_with_two_generations : an_event_types_storage
{
    protected static readonly EventType first_generation = new("some-event", EventTypeGeneration.First);
    protected static readonly EventType second_generation = new("some-event", new EventTypeGeneration(2));

    async Task Establish()
    {
        await _storage.Register(first_generation, new JsonSchema { Description = "first" });
        await _storage.Register(second_generation, new JsonSchema { Description = "second" });
    }
}
