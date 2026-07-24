// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_registering_a_new_generation_after_caching(MongoDBFixture fixture)
    : given.an_event_types_storage(fixture)
{
    const string FirstSchema = """{ "type": "object", "properties": { "name": { "type": "string" } } }""";
    const string SecondSchema = """{ "type": "object", "properties": { "name": { "type": "string" }, "age": { "type": "integer" } } }""";

    static readonly EventTypeGeneration _secondGeneration = new(2U);

    EventTypeDefinition _definitionBeforeNewGeneration;
    EventTypeDefinition _definitionAfterNewGeneration;
    EventTypeSchema _schemaAfterEviction;

    async Task Because()
    {
        await RegisterGeneration(EventTypeGeneration.First, FirstSchema);

        _ = await _storage.GetFor(_eventTypeId);
        _definitionBeforeNewGeneration = await _storage.GetDefinition(_eventTypeId);

        await RegisterGeneration(_secondGeneration, SecondSchema);

        ClearRecordedCalls();
        _schemaAfterEviction = await _storage.GetFor(_eventTypeId);
        _definitionAfterNewGeneration = await _storage.GetDefinition(_eventTypeId);
    }

    [Fact] void should_expose_only_the_first_generation_before_the_new_one() =>
        _definitionBeforeNewGeneration.Generations.Count().ShouldEqual(1);

    [Fact] void should_re_read_the_collection_after_eviction() => ShouldHaveQueriedTheCollection();

    [Fact] void should_still_serve_the_first_generation_schema() =>
        _schemaAfterEviction.Type.Generation.ShouldEqual(EventTypeGeneration.First);

    [Fact] void should_expose_both_generations_after_registration() =>
        _definitionAfterNewGeneration.Generations.Count().ShouldEqual(2);
}
