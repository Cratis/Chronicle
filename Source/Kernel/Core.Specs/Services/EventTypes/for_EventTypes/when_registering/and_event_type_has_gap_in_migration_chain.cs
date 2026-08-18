// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_event_type_has_gap_in_migration_chain : given.all_dependencies
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
                        new EventTypeMigrationDefinition { FromGeneration = 1, ToGeneration = 2 }
                    }
                }
            ]
        }));

    [Fact] void should_throw_missing_migration_for_generation() => _exception.ShouldBeOfExactType<MissingMigrationForEventTypeGeneration>();
}
