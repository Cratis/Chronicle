// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_event_type_is_missing_migrator_from_first_generation : given.all_dependencies
{
    Exception _exception;

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(new RegisterEventTypesRequest
        {
            EventStore = "test-store",
            Types =
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 3 },
                    Schema = "{}",
                    Migrations =
                    {
                        new EventTypeMigrationDefinition { FromGeneration = 2, ToGeneration = 3 }
                    }
                }
            ]
        }));

    [Fact] void should_throw_missing_first_generation() => _exception.ShouldBeOfExactType<MissingFirstGenerationForEventType>();
}
