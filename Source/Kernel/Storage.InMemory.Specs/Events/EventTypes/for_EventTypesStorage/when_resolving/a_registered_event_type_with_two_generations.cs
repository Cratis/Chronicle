// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_resolving;

public class a_registered_event_type_with_two_generations : given.an_event_type_with_two_generations
{
    EventTypeSchema _withoutGeneration;
    EventTypeSchema _firstGeneration;
    EventTypeSchema _secondGeneration;
    IEnumerable<EventTypeSchema> _latest;
    IEnumerable<EventTypeSchema> _byGeneration;
    IEnumerable<EventTypeSchema> _byId;

    async Task Because()
    {
        _withoutGeneration = await _storage.GetFor(first_generation.Id);
        _firstGeneration = await _storage.GetFor(first_generation.Id, 1);
        _secondGeneration = await _storage.GetFor(first_generation.Id, 2);
        _latest = await _storage.GetLatestForAllEventTypes();
        _byGeneration = await _storage.GetFor([first_generation, second_generation]);
        _byId = await _storage.GetFor([first_generation.Id]);
    }

    [Fact] void should_resolve_the_latest_generation_when_none_is_given() => _withoutGeneration.Type.Generation.ShouldEqual(second_generation.Generation);
    [Fact] void should_resolve_the_first_generation_schema() => _firstGeneration.Schema.Description.ShouldEqual("first");
    [Fact] void should_resolve_the_second_generation_schema() => _secondGeneration.Schema.Description.ShouldEqual("second");
    [Fact] void should_only_hold_the_latest_generation_among_all_latest() => _latest.Single().Type.Generation.ShouldEqual(second_generation.Generation);
    [Fact] void should_resolve_each_requested_generation() => _byGeneration.Select(_ => _.Type.Generation).ShouldContainOnly(first_generation.Generation, second_generation.Generation);
    [Fact] void should_resolve_the_latest_generation_by_id() => _byId.Single().Type.Generation.ShouldEqual(second_generation.Generation);
}
