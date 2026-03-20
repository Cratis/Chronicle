// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_schema_has_changed_for_existing_generation : given.all_dependencies
{
    Exception _exception;
    const string OriginalSchema = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string ModifiedSchema = """{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""";

    void Establish()
    {
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(true);
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(Task.FromResult(new Concepts.EventTypes.EventTypeSchema(
                new Concepts.Events.EventType("some-event", 1),
                Concepts.Events.EventTypeOwner.Client,
                Concepts.Events.EventTypeSource.Code,
                JsonSchema.FromJsonAsync(OriginalSchema).GetAwaiter().GetResult())));
    }

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(new RegisterEventTypesRequest
        {
            EventStore = "test-store",
            Types =
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
            ]
        }));

    [Fact] void should_throw_event_type_schema_changed() => _exception.ShouldBeOfExactType<EventTypeSchemaChanged>();
}
