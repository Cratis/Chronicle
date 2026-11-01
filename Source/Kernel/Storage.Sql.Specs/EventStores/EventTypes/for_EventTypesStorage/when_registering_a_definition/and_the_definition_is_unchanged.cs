// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_registering_a_definition;

public class and_the_definition_is_unchanged : given.an_event_types_storage
{
    EventTypeDefinition _definition;
    bool _changed;
    IDictionary<uint, string> _stored;

    async Task Establish()
    {
        var first = await JsonSchema.FromJsonAsync(SchemaFor(FirstGenerationProperty));
        var second = await JsonSchema.FromJsonAsync(SchemaFor(SecondGenerationProperty));
        _definition = new EventTypeDefinition(
            _eventTypeId,
            EventTypeOwner.None,
            false,
            [new EventTypeGenerationDefinition(_firstGeneration, first), new EventTypeGenerationDefinition(_secondGeneration, second)],
            []);
    }

    async Task Because()
    {
        _changed = await _storage.Register(_definition);
        await using var context = CreateContext();
        _stored = (await context.EventTypes.SingleAsync()).Schemas;
    }

    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] void should_keep_both_generations() => _stored.Keys.ShouldContainOnly((uint)_firstGeneration, (uint)_secondGeneration);
    [Fact] void should_keep_the_first_schema() => _stored[_firstGeneration].ShouldEqual(SchemaFor(FirstGenerationProperty));
    [Fact] void should_keep_the_second_schema() => _stored[_secondGeneration].ShouldEqual(SchemaFor(SecondGenerationProperty));
}
