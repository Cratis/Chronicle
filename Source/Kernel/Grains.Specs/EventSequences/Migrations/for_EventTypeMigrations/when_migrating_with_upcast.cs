// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using NJsonSchema;

namespace Cratis.Chronicle.Grains.EventSequences.Migrations.for_EventTypeMigrations;

public class when_migrating_with_upcast : given.all_dependencies
{
    IDictionary<EventTypeGeneration, ExpandoObject> _result;
    EventTypeDefinition _definition;
    ExpandoObject _gen1ExpandoObject;
    ExpandoObject _gen2ExpandoObject;
    JsonObject _upcastJmesPath;

    async Task Establish()
    {
        // Event starts at generation 1, should upcast to generation 2
        _eventType = new EventType(Guid.NewGuid().ToString(), 1);

        var gen1Schema = await JsonSchema.FromJsonAsync("{}");
        var gen2Schema = await JsonSchema.FromJsonAsync("{}");

        _upcastJmesPath = new JsonObject { ["firstName"] = "@.name" };

        _definition = new EventTypeDefinition(
            _eventType.Id,
            EventTypeOwner.None,
            false,
            [
                new EventTypeGenerationDefinition(1, gen1Schema),
                new EventTypeGenerationDefinition(2, gen2Schema)
            ],
            [
                new EventTypeMigrationDefinition(
                    1,
                    2,
                    [],
                    _upcastJmesPath,
                    new JsonObject { ["name"] = "@.firstName" })
            ]);

        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(_definition);

        _gen1ExpandoObject = new ExpandoObject();
        _gen2ExpandoObject = new ExpandoObject();

        _expandoObjectConverter
            .ToExpandoObject(Arg.Any<JsonObject>(), gen1Schema)
            .Returns(_gen1ExpandoObject);

        _expandoObjectConverter
            .ToExpandoObject(Arg.Any<JsonObject>(), gen2Schema)
            .Returns(_gen2ExpandoObject);
    }

    async Task Because() => _result = await _eventTypeMigrations.MigrateToAllGenerations(_eventType, _content);

    [Fact] void should_return_two_generations() => _result.Count.ShouldEqual(2);

    [Fact] void should_contain_source_generation() => _result.ContainsKey(1).ShouldBeTrue();

    [Fact] void should_contain_upcasted_generation() => _result.ContainsKey(2).ShouldBeTrue();

    [Fact] void should_return_gen1_expando_object() => _result[(EventTypeGeneration)1].ShouldEqual(_gen1ExpandoObject);

    [Fact] void should_return_gen2_expando_object() => _result[(EventTypeGeneration)2].ShouldEqual(_gen2ExpandoObject);
}
