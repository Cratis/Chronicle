// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

/// <summary>
/// Resolving a nested path must still reject one that goes nowhere. A default value used to skip validation
/// outright, which let a path the kernel could not honour through to a silent drop at conversion time (#3949).
/// </summary>
public class and_a_migration_targets_a_nested_property_that_does_not_exist : given.all_dependencies
{
    Exception _exception;

    const string SchemaGen1 = """{"type":"object","properties":{"price":{"type":"object","properties":{"amount":{"type":"number"}}}}}""";
    const string SchemaGen2 = """{"type":"object","properties":{"price":{"type":"object","properties":{"amount":{"type":"number"}}}}}""";

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(
            "test-store",
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
                            UpcastJmesPath = """{"price.nonExistent":{"$defaultValue":"unspecified"}}""",
                            DowncastJmesPath = "{}"
                        }
                    },
                    Generations =
                    {
                        new EventTypeGenerationDefinition { Generation = 1, Schema = SchemaGen1 },
                        new EventTypeGenerationDefinition { Generation = 2, Schema = SchemaGen2 }
                    }
                }
            ],
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture));

    [Fact] void should_throw_invalid_migration_property() => _exception.ShouldBeOfExactType<InvalidMigrationPropertyForEventType>();
}
