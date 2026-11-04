// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_registering_a_definition;

public class and_an_existing_generation_changes : given.an_event_types_storage
{
    EventTypeDefinition _definition;
    JsonSchema _changedSchema;
    bool _changed;
    IDictionary<uint, string> _stored;

    async Task Establish()
    {
        _changedSchema = await JsonSchema.FromJsonAsync(SchemaFor("changedValue"));
        _definition = new EventTypeDefinition(
            _eventTypeId,
            EventTypeOwner.None,
            false,
            [new EventTypeGenerationDefinition(_firstGeneration, _changedSchema)],
            []);
    }

    async Task Because()
    {
        _changed = await _storage.Register(_definition);
        await using var context = CreateContext();
        _stored = (await context.EventTypes.SingleAsync()).Schemas;
    }

    [Fact] void should_report_a_change() => _changed.ShouldBeTrue();
    [Fact] void should_keep_both_generations() => _stored.Keys.ShouldContainOnly((uint)_firstGeneration, (uint)_secondGeneration);
    [Fact] void should_store_the_changed_schema() => _stored[_firstGeneration].ShouldEqual(_changedSchema.ToJson());
    [Fact] void should_keep_the_omitted_schema() => _stored[_secondGeneration].ShouldEqual(SchemaFor(SecondGenerationProperty));
}
