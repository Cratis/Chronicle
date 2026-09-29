// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_getting_for_event_types_of_different_generations : given.an_event_type_with_two_generations
{
    IEnumerable<EventTypeSchema> _schemas;

    async Task Because() => _schemas = await _storage.GetFor([second_generation, first_generation]);

    [Fact] void should_return_one_schema_per_requested_event_type() => _schemas.Count().ShouldEqual(2);
    [Fact] void should_return_the_schema_of_the_first_generation_for_it() => _schemas.Single(_ => _.Type.Generation == first_generation.Generation).Schema.Description.ShouldEqual("first");
    [Fact] void should_return_the_schema_of_the_second_generation_for_it() => _schemas.Single(_ => _.Type.Generation == second_generation.Generation).Schema.Description.ShouldEqual("second");
}
