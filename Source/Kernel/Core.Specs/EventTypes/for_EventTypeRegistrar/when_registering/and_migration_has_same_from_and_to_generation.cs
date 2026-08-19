// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

internal class and_migration_has_same_from_and_to_generation : given.all_dependencies
{
    Exception _exception;

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register("test-store", [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 2 },
                    Schema = "{}",
                    Migrations =
                    {
                        new EventTypeMigrationDefinition { FromGeneration = 1, ToGeneration = 1, UpcastJmesPath = "{}", DowncastJmesPath = "{}" },
                        new EventTypeMigrationDefinition { FromGeneration = 1, ToGeneration = 2, UpcastJmesPath = "{}", DowncastJmesPath = "{}" }
                    },
                    Generations =
                    {
                        new EventTypeGenerationDefinition { Generation = 1, Schema = "{}" },
                        new EventTypeGenerationDefinition { Generation = 2, Schema = "{}" }
                    }
                }
            ], false, _storage, _eventTypesCacheClient));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
