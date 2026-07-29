// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_a_new_generation_for_an_existing_event_type : given.an_event_types_storage
{
    IEnumerable<EventTypeDefinition> _definitions;

    async Task Because()
    {
        await _storage.Register(new EventType("some-event", EventTypeGeneration.First), new JsonSchema());
        await _storage.Register(new EventType("some-event", new EventTypeGeneration(2)), new JsonSchema());
        _definitions = await _storage.GetAllDefinitions();
    }

    [Fact] void should_keep_both_generations() =>
        _definitions.First().Generations.Select(_ => _.Generation)
            .ShouldContainOnly(EventTypeGeneration.First, new EventTypeGeneration(2));
}
