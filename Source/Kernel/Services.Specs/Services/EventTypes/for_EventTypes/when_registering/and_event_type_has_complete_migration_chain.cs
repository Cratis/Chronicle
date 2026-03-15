// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_event_type_has_complete_migration_chain : given.all_dependencies
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
                    Type = new() { Id = "some-event", Generation = 2 },
                    Schema = "{}",
                    Migrations =
                    {
                        new EventTypeMigrationDefinition { FromGeneration = 1, ToGeneration = 2, UpcastJmesPath = "{}", DowncastJmesPath = "{}" }
                    },
                    Generations =
                    {
                        new EventTypeGenerationDefinition { Generation = 1, Schema = "{}" },
                        new EventTypeGenerationDefinition { Generation = 2, Schema = "{}" }
                    }
                }
            ]
        }));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
