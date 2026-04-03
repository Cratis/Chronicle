// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_schema_is_unchanged_for_existing_generation : given.all_dependencies
{
    Exception _exception;
    const string Schema = """{"type":"object","properties":{"name":{"type":"string"}}}""";

    void Establish()
    {
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(true);
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(Task.FromResult(new Concepts.EventTypes.EventTypeSchema(
                new Concepts.Events.EventType("some-event", 1),
                Concepts.Events.EventTypeOwner.Client,
                Concepts.Events.EventTypeSource.Code,
                JsonSchema.FromJsonAsync(Schema).GetAwaiter().GetResult())));
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
                    Schema = Schema,
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = Schema }
                    }
                }
            ]
        }));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
