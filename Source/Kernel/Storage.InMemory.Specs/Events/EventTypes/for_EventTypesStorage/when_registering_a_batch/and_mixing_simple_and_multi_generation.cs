// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_registering_a_batch;

public class and_mixing_simple_and_multi_generation : given.a_batch_of_event_types
{
    EventTypeSchema _simpleSchema;
    EventTypeDefinition _migrated;

    async Task Because()
    {
        await _storage.Register(_batch);
        _simpleSchema = await _storage.GetFor(new EventTypeId("simple-event"));
        _migrated = await _storage.GetDefinition(new EventTypeId("migrated-event"));
    }

    [Fact] void should_record_the_visibility_of_the_simple_type() => _simpleSchema.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_record_the_origin_of_the_simple_type() => _simpleSchema.Origin.ShouldEqual("an-origin");
    [Fact] void should_record_the_source_of_the_simple_type() => _simpleSchema.Source.ShouldEqual(EventTypeSource.Code);
    [Fact] void should_keep_every_generation_of_the_multi_generation_type() => _migrated.Generations.Count().ShouldEqual(2);
    [Fact] void should_keep_the_migrations_of_the_multi_generation_type() => _migrated.Migrations.Count().ShouldEqual(1);
}
