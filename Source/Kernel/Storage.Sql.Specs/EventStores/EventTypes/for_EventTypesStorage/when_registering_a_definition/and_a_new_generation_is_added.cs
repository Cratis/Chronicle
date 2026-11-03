// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_registering_a_definition;

public class and_a_new_generation_is_added : given.an_event_types_storage
{
    static readonly EventTypeGeneration _thirdGeneration = new(3);
    EventTypeDefinition _definition;
    EventTypeDefinition _cachedDefinition;
    JsonSchema _newSchema;
    bool _changed;
    IDictionary<uint, string> _stored;

    async Task Establish()
    {
        await _storage.GetDefinition(_eventTypeId);
        _newSchema = await JsonSchema.FromJsonAsync(SchemaFor("thirdGenerationValue"));
        _definition = new EventTypeDefinition(
            _eventTypeId,
            EventTypeOwner.None,
            false,
            [new EventTypeGenerationDefinition(_thirdGeneration, _newSchema)],
            []);
    }

    async Task Because()
    {
        _changed = await _storage.Register(_definition);
        await using var context = CreateContext();
        _stored = (await context.EventTypes.SingleAsync()).Schemas;
        _cachedDefinition = await _storage.GetDefinition(_eventTypeId);
    }

    [Fact] void should_report_a_change() => _changed.ShouldBeTrue();
    [Fact] void should_keep_all_three_generations() => _stored.Keys.ShouldContainOnly((uint)_firstGeneration, (uint)_secondGeneration, (uint)_thirdGeneration);
    [Fact] void should_keep_the_first_schema() => _stored[_firstGeneration].ShouldEqual(SchemaFor(FirstGenerationProperty));
    [Fact] void should_keep_the_second_schema() => _stored[_secondGeneration].ShouldEqual(SchemaFor(SecondGenerationProperty));
    [Fact] void should_store_the_new_schema() => _stored[_thirdGeneration].ShouldEqual(_newSchema.ToJson());
    [Fact] void should_refresh_the_cached_generations() => _cachedDefinition.Generations.Select(_ => _.Generation).ShouldContainOnly(_firstGeneration, _secondGeneration, _thirdGeneration);
    [Fact] void should_refresh_the_cached_schema() => _cachedDefinition.Generations.Single(_ => _.Generation == _thirdGeneration).Schema.ToJson().ShouldEqual(_newSchema.ToJson());
}
