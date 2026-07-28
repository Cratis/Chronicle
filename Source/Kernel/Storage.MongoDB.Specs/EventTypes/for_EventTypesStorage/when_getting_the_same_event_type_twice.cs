// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_getting_the_same_event_type_twice(MongoDBFixture fixture)
    : given.an_event_types_storage(fixture)
{
    const string Schema = """{ "type": "object", "properties": { "name": { "type": "string" } } }""";

    EventTypeSchema _first;
    EventTypeSchema _second;

    async Task Because()
    {
        await RegisterGeneration(EventTypeGeneration.First, Schema);

        _first = await _storage.GetFor(_eventTypeId);
        ClearRecordedCalls();
        _second = await _storage.GetFor(_eventTypeId);
    }

    [Fact] void should_return_the_same_schema_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();

    [Fact] void should_return_the_same_json_schema_instance() => ReferenceEquals(_first.Schema, _second.Schema).ShouldBeTrue();

    [Fact] void should_not_query_the_collection_again() => ShouldNotHaveQueriedTheCollection();
}
