// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaComparison;

public class when_finding_problem : Specification
{
    const string StoredSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";
    const string ChangedSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"integer\"}}}";

    static Concepts.Events.EventTypeDefinition Stored(uint generation) =>
        new(
            "some-event",
            Concepts.Events.EventTypeOwner.Client,
            false,
            [new Concepts.Events.EventTypeGenerationDefinition(generation, JsonSchema.FromJsonAsync(StoredSchema).GetAwaiter().GetResult())],
            []);

    static EventTypeRegistration Legacy(uint generation, string schema) =>
        new() { Type = new() { Id = "some-event", Generation = generation }, Schema = schema };

    static EventTypeRegistration WithGeneration(uint generation, string schema)
    {
        var registration = Legacy(1, "{}");
        registration.Generations.Add(new Contracts.Events.EventTypeGenerationDefinition { Generation = generation, Schema = schema });
        return registration;
    }

    [Fact] async Task should_find_nothing_without_a_stored_definition() =>
        (await EventTypeSchemaComparison.FindProblem(Legacy(1, ChangedSchema), null)).ShouldBeNull();

    [Fact] async Task should_find_nothing_for_an_unchanged_schema() =>
        (await EventTypeSchemaComparison.FindProblem(Legacy(1, StoredSchema), Stored(1))).ShouldBeNull();

    [Fact] async Task should_find_nothing_for_a_generation_not_stored() =>
        (await EventTypeSchemaComparison.FindProblem(Legacy(2, ChangedSchema), Stored(1))).ShouldBeNull();

    [Fact] async Task should_find_the_generation_of_a_changed_legacy_schema()
    {
        var problem = await EventTypeSchemaComparison.FindProblem(Legacy(1, ChangedSchema), Stored(1));
        problem!.Generation.ShouldEqual(1u);
    }

    [Fact] async Task should_find_the_difference_of_a_changed_generation()
    {
        var problem = await EventTypeSchemaComparison.FindProblem(WithGeneration(1, ChangedSchema), Stored(1));
        problem!.Difference.ShouldNotBeNull();
    }

    [Fact] async Task should_find_nothing_for_an_unchanged_generation() =>
        (await EventTypeSchemaComparison.FindProblem(WithGeneration(1, StoredSchema), Stored(1))).ShouldBeNull();
}
