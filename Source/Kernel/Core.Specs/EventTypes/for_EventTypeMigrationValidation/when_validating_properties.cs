// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeMigrationValidation;

public class when_validating_properties : Specification
{
    const string FirstSchema = "{\"type\":\"object\",\"properties\":{\"oldName\":{\"type\":\"string\"}}}";
    const string SecondSchema = "{\"type\":\"object\",\"properties\":{\"newName\":{\"type\":\"string\"}}}";

    static List<EventTypeGenerationDefinition> Generations() =>
    [
        new(1u, JsonSchema.FromJsonAsync(FirstSchema).GetAwaiter().GetResult()),
        new(2u, JsonSchema.FromJsonAsync(SecondSchema).GetAwaiter().GetResult())
    ];

    static EventTypeMigrationDefinition Migration(string upcast, string downcast) =>
        new(1u, 2u, [], JsonNode.Parse(upcast)!.AsObject(), JsonNode.Parse(downcast)!.AsObject());

    static Exception? Validate(string upcast, string downcast) =>
        Catch.Exception(() => EventTypeMigrationValidation.ValidateProperties("some-event", Migration(upcast, downcast), Generations()));

    [Fact] void should_accept_valid_keys_and_sources() =>
        Validate("{\"newName\":{\"$rename\":\"oldName\"}}", "{\"oldName\":{\"$rename\":\"newName\"}}").ShouldBeNull();

    [Fact] void should_reject_an_upcast_key_missing_from_the_target() =>
        Validate("{\"missing\":\"@.oldName\"}", "{}").ShouldBeOfExactType<InvalidMigrationPropertyForEventType>();

    [Fact] void should_reject_a_downcast_key_missing_from_the_source() =>
        Validate("{}", "{\"missing\":\"@.newName\"}").ShouldBeOfExactType<InvalidMigrationPropertyForEventType>();

    [Fact] void should_reject_an_upcast_source_missing_from_the_source_generation() =>
        Validate("{\"newName\":\"@.missing\"}", "{}").ShouldBeOfExactType<InvalidMigrationPropertyForEventType>();

    [Fact] void should_reject_a_downcast_source_missing_from_the_target_generation() =>
        Validate("{}", "{\"oldName\":\"@.missing\"}").ShouldBeOfExactType<InvalidMigrationPropertyForEventType>();

    [Fact] void should_accept_a_default_value_for_a_key_when_the_schema_is_empty()
    {
        var generations = Generations();
        generations[1] = new(2u, new JsonSchema());
        var migration = Migration("{\"added\":{\"$defaultValue\":1}}", "{}");

        Catch.Exception(() => EventTypeMigrationValidation.ValidateProperties("some-event", migration, generations)).ShouldBeNull();
    }
}
