// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_getting_for_an_event_type_of_a_generation_that_is_not_registered : given.an_event_type_with_two_generations
{
    static readonly EventType unregistered_generation = new("some-event", new EventTypeGeneration(7));
    EventTypeSchema _schema;

    async Task Because() => _schema = (await _storage.GetFor([unregistered_generation])).Single();

    [Fact] void should_keep_the_generation_it_was_asked_for() => _schema.Type.Generation.ShouldEqual(unregistered_generation.Generation);
    [Fact] void should_fall_back_to_the_latest_schema() => _schema.Schema.Description.ShouldEqual("second");
}
