// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_the_same_event_type_twice : given.an_event_types_storage
{
    IEnumerable<EventTypeDefinition> _definitions;

    async Task Because()
    {
        await _storage.Register(new EventType("some-event", EventTypeGeneration.First), new JsonSchema());
        await _storage.Register(new EventType("some-event", EventTypeGeneration.First), new JsonSchema());
        _definitions = await _storage.GetAllDefinitions();
    }

    [Fact] void should_only_have_it_once() => _definitions.Count().ShouldEqual(1);
    [Fact] void should_only_have_the_first_generation() =>
        _definitions.First().Generations.Select(_ => _.Generation).ShouldContainOnly(EventTypeGeneration.First);
}
