// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3949 — a migration built from a nested property
/// expression carries a path rather than a name, and resolving it against the top-level property map alone
/// reported that the property did not exist when it existed one level down.
/// </summary>
public class and_a_migration_targets_a_nested_property : given.all_dependencies
{
    Exception _exception;

    const string SchemaGen1 = """{"type":"object","properties":{"price":{"type":"object","properties":{"amount":{"type":"number"}}}}}""";
    const string SchemaGen2 = """{"type":"object","properties":{"price":{"type":"object","properties":{"amount":{"type":"number"},"description":{"type":"string"}}}}}""";

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
                            UpcastJmesPath = """{"price.description":{"$defaultValue":"unspecified"}}""",
                            DowncastJmesPath = """{"price.amount":"@.price.amount"}"""
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

    [Fact] void should_accept_the_nested_path() => _exception.ShouldBeNull();
}
