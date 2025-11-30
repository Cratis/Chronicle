// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using NJsonSchema;

namespace Cratis.Chronicle.Grains.EventSequences.Migrations.for_EventTypeMigrations;

public class when_migrating_with_single_generation : given.all_dependencies
{
    IDictionary<EventTypeGeneration, ExpandoObject> _result;
    EventTypeDefinition _definition;
    ExpandoObject _expectedExpandoObject;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("{}");
        _definition = new EventTypeDefinition(
            _eventType.Id,
            EventTypeOwner.None,
            false,
            [new EventTypeGenerationDefinition(1, schema)],
            []);

        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(_definition);

        _expectedExpandoObject = new ExpandoObject();
        _expandoObjectConverter
            .ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_expectedExpandoObject);
    }

    async Task Because() => _result = await _eventTypeMigrations.MigrateToAllGenerations(_eventType, _content);

    [Fact] void should_return_single_generation() => _result.Count.ShouldEqual(1);

    [Fact] void should_contain_source_generation() => _result.ContainsKey(_eventType.Generation).ShouldBeTrue();

    [Fact] void should_return_converted_expando_object() => _result[_eventType.Generation].ShouldEqual(_expectedExpandoObject);
}
