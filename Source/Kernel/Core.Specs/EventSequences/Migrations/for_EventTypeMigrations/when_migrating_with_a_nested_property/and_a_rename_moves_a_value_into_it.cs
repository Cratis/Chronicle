// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_migrating_with_a_nested_property;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3949 — a rename addressing either end of the move by
/// a nested path resolved to nothing, so restructuring a payload could not be expressed at all.
/// </summary>
public class and_a_rename_moves_a_value_into_it : given.all_dependencies
{
    JsonObject _upcasted;

    async Task Establish()
    {
        _eventType = new EventType(Guid.NewGuid().ToString(), 1);
        _content = new JsonObject { ["amount"] = 42 };

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
                        ["price.amount"] = new JsonObject { [WellKnownExpressions.Rename] = "amount" }
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
    [Fact] void should_write_the_value_into_the_nested_object() => _upcasted["price"]!["amount"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_not_write_a_dotted_top_level_key() => _upcasted.ContainsKey("price.amount").ShouldBeFalse();
}
