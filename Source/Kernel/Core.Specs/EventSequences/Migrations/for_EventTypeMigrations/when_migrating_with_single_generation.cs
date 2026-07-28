// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations;

public class when_migrating_with_single_generation : given.all_dependencies
{
    IDictionary<EventTypeGeneration, ExpandoObject> _result;
    EventTypeDefinition _definition;

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
    }

    async Task Because() => _result = await _eventTypeMigrations.MigrateToAllGenerations(_eventStoreName, _eventType, _content, _contentAsExpandoObject);

    [Fact] void should_return_single_generation() => _result.Count.ShouldEqual(1);

    [Fact] void should_contain_source_generation() => _result.ContainsKey(_eventType.Generation).ShouldBeTrue();

    [Fact] void should_reuse_the_already_built_expando_object() => _result[_eventType.Generation].ShouldBeSame(_contentAsExpandoObject);

    [Fact] void should_not_convert_the_content_again() => _expandoObjectConverter.DidNotReceive().ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>());
}
