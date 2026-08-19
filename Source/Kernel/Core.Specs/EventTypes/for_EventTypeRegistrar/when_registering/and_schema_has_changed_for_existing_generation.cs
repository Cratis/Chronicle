// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_schema_has_changed_for_existing_generation : given.all_dependencies
{
    Exception _exception;
    const string OriginalSchema = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string ModifiedSchema = """{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""";

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, OriginalSchema)));

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(
            "test-store",
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1 },
                    Schema = ModifiedSchema,
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = ModifiedSchema }
                    }
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient));

    [Fact] void should_throw_event_type_schema_changed() => _exception.ShouldBeOfExactType<EventTypeSchemaChanged>();
}
