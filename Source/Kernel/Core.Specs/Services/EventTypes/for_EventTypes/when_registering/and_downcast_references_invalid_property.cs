// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

public class and_downcast_references_invalid_property : given.all_dependencies
{
    Exception _exception;
    const string SchemaGen1 = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string SchemaGen2 = """{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""";

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(new RegisterEventTypesRequest
        {
            EventStore = "test-store",
            Types =
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 2 },
                    Schema = SchemaGen2,
                    Migrations =
                    {
                        new EventTypeMigrationDefinition
                        {
                            FromGeneration = 1,
                            ToGeneration = 2,
                            UpcastJmesPath = """{"age":"@.name"}""",
                            DowncastJmesPath = """{"nonExistent":"@.age"}"""
                        }
                    },
                    Generations =
                    {
                        new EventTypeGenerationDefinition { Generation = 1, Schema = SchemaGen1 },
                        new EventTypeGenerationDefinition { Generation = 2, Schema = SchemaGen2 }
                    }
                }
            ]
        }));

    [Fact] void should_throw_invalid_migration_property() => _exception.ShouldBeOfExactType<InvalidMigrationPropertyForEventType>();
}
