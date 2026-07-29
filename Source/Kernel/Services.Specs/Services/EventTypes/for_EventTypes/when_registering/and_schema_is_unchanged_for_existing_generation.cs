// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_schema_is_unchanged_for_existing_generation : given.all_dependencies
{
    Exception _exception;
    const string Schema = """{"type":"object","properties":{"name":{"type":"string"}}}""";

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, Schema)));

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
