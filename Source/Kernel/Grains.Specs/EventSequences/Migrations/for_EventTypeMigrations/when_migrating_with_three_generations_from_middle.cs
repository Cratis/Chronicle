// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using NJsonSchema;

namespace Cratis.Chronicle.Grains.EventSequences.Migrations.for_EventTypeMigrations;

public class when_migrating_with_three_generations_from_middle : given.all_dependencies
{
    IDictionary<EventTypeGeneration, ExpandoObject> _result;
    EventTypeDefinition _definition;
    ExpandoObject _gen1ExpandoObject;
    ExpandoObject _gen2ExpandoObject;
    ExpandoObject _gen3ExpandoObject;

    async Task Establish()
    {
        // Event starts at generation 2, should upcast to 3 and downcast to 1
        _eventType = new EventType(Guid.NewGuid().ToString(), 2);

        var gen1Schema = await JsonSchema.FromJsonAsync("{}");
        var gen2Schema = await JsonSchema.FromJsonAsync("{}");
        var gen3Schema = await JsonSchema.FromJsonAsync("{}");

        _definition = new EventTypeDefinition(
            _eventType.Id,
            EventTypeOwner.None,
            false,
            [
                new EventTypeGenerationDefinition(1, gen1Schema),
                new EventTypeGenerationDefinition(2, gen2Schema),
                new EventTypeGenerationDefinition(3, gen3Schema)
            ],
            [
                new EventTypeMigrationDefinition(
                    1,
                    2,
                    [],
                    new JsonObject { ["firstName"] = "@.name" },
                    new JsonObject { ["name"] = "@.firstName" }),
                new EventTypeMigrationDefinition(
                    2,
                    3,
                    [],
                    new JsonObject { ["fullName"] = "@.firstName" },
                    new JsonObject { ["firstName"] = "@.fullName" })
            ]);

        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(_definition);

        _gen1ExpandoObject = new ExpandoObject();
        _gen2ExpandoObject = new ExpandoObject();
        _gen3ExpandoObject = new ExpandoObject();

        _expandoObjectConverter
            .ToExpandoObject(Arg.Any<JsonObject>(), gen1Schema)
            .Returns(_gen1ExpandoObject);

        _expandoObjectConverter
            .ToExpandoObject(Arg.Any<JsonObject>(), gen2Schema)
            .Returns(_gen2ExpandoObject);

        _expandoObjectConverter
            .ToExpandoObject(Arg.Any<JsonObject>(), gen3Schema)
            .Returns(_gen3ExpandoObject);
    }

    async Task Because() => _result = await _eventTypeMigrations.MigrateToAllGenerations(_eventType, _content);

    [Fact] void should_return_three_generations() => _result.Count.ShouldEqual(3);

    [Fact] void should_contain_generation_1() => _result.ContainsKey(1).ShouldBeTrue();

    [Fact] void should_contain_generation_2() => _result.ContainsKey(2).ShouldBeTrue();

    [Fact] void should_contain_generation_3() => _result.ContainsKey(3).ShouldBeTrue();

    [Fact] void should_return_gen1_expando_object() => _result[(EventTypeGeneration)1].ShouldEqual(_gen1ExpandoObject);

    [Fact] void should_return_gen2_expando_object() => _result[(EventTypeGeneration)2].ShouldEqual(_gen2ExpandoObject);

    [Fact] void should_return_gen3_expando_object() => _result[(EventTypeGeneration)3].ShouldEqual(_gen3ExpandoObject);
}
