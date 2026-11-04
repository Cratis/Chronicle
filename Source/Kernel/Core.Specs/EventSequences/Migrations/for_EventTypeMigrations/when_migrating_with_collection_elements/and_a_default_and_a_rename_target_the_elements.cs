// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_migrating_with_collection_elements;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/5614 - a property inside the elements of a collection
/// used to be written over the collection itself, replacing the list with a single object.
/// </summary>
public class and_a_default_and_a_rename_target_the_elements : given.all_dependencies
{
    JsonObject _upcasted;

    async Task Establish()
    {
        _eventType = new EventType(Guid.NewGuid().ToString(), 1);
        _content = new JsonObject
        {
            ["days"] = new JsonArray(
                new JsonObject { ["hours"] = 8, ["note"] = "first" },
                new JsonObject { ["hours"] = 6, ["note"] = "second", ["activity"] = "existing" })
        };

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
                        ["days[].activity"] = new JsonObject { [WellKnownExpressions.DefaultValue] = "work" },
                        ["days[].comment"] = new JsonObject { [WellKnownExpressions.Rename] = "days[].note" }
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

    [Fact] void should_keep_the_collection() => _upcasted["days"]!.AsArray().Count.ShouldEqual(2);
    [Fact] void should_default_the_element_without_a_value() => _upcasted["days"]![0]!["activity"]!.GetValue<string>().ShouldEqual("work");
    [Fact] void should_keep_an_existing_value() => _upcasted["days"]![1]!["activity"]!.GetValue<string>().ShouldEqual("existing");
    [Fact] void should_rename_within_each_element() => _upcasted["days"]![1]!["comment"]!.GetValue<string>().ShouldEqual("second");
    [Fact] void should_keep_the_other_properties() => _upcasted["days"]![0]!["hours"]!.GetValue<int>().ShouldEqual(8);
}
