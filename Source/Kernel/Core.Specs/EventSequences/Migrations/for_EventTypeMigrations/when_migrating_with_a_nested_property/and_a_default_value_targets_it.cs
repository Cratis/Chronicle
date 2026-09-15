// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_migrating_with_a_nested_property;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3949 — a default value targeting a property inside a
/// nested object used to be written as a top-level key literally named "price.description", which the target
/// generation's schema then discarded. The failure was silent: registration succeeded and the value simply never
/// appeared.
/// </summary>
public class and_a_default_value_targets_it : given.all_dependencies
{
    JsonObject _upcasted;

    async Task Establish()
    {
        _eventType = new EventType(Guid.NewGuid().ToString(), 1);
        _content = new JsonObject { ["price"] = new JsonObject { ["amount"] = 42 } };

        var gen1Schema = await JsonSchema.FromJsonAsync("{}");
        var gen2Schema = await JsonSchema.FromJsonAsync("{}");

        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(new EventTypeDefinition(
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
                    new JsonObject
                    {
                        ["price.description"] = new JsonObject { [WellKnownExpressions.DefaultValue] = "unspecified" }
                    },
                    [])
            ]));

        _expandoObjectConverter
            .ToExpandoObject(Arg.Any<JsonObject>(), gen2Schema)
            .Returns(callInfo =>
            {
                _upcasted = callInfo.Arg<JsonObject>();
                return new ExpandoObject();
            });
    }

    async Task Because() => await _eventTypeMigrations.MigrateToAllGenerations(_eventStoreName, _eventType, _content, _contentAsExpandoObject);

    [Fact] void should_have_upcasted() => _upcasted.ShouldNotBeNull();
    [Fact] void should_write_the_default_inside_the_nested_object() => _upcasted["price"]!["description"]!.GetValue<string>().ShouldEqual("unspecified");
    [Fact] void should_not_write_a_dotted_top_level_key() => _upcasted.ContainsKey("price.description").ShouldBeFalse();
    [Fact] void should_keep_the_existing_nested_value() => _upcasted["price"]!["amount"]!.GetValue<int>().ShouldEqual(42);
}
