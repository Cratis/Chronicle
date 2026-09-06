// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_migrating_with_a_value_map.given;

/// <summary>
/// A two-generation event type whose <c>status</c> enumeration was renumbered between the generations, with the
/// value map a migration declares to say what the old numbers became - and its inverse for the way back.
/// </summary>
public class a_definition_with_a_value_map : Migrations.for_EventTypeMigrations.given.all_dependencies
{
    protected ExpandoObject _gen1ExpandoObject;
    protected ExpandoObject _gen2ExpandoObject;
    protected IDictionary<EventTypeGeneration, ExpandoObject> _result;

    /// <summary>
    /// Gets the generation the event being migrated was stored at, which decides whether the map is exercised
    /// forward or inverted.
    /// </summary>
    protected virtual EventTypeGeneration StoredAtGeneration => 1;

    async Task Establish()
    {
        _eventType = new EventType(_eventType.Id, StoredAtGeneration);

        var gen1Schema = await JsonSchema.FromJsonAsync("{}");
        var gen2Schema = await JsonSchema.FromJsonAsync("{}");

        var definition = new EventTypeDefinition(
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
                    ValueMap(0, 10),
                    ValueMap(10, 0))
            ]);

        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(definition);

        _gen1ExpandoObject = new ExpandoObject();
        _gen2ExpandoObject = new ExpandoObject();

        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), gen1Schema).Returns(_gen1ExpandoObject);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), gen2Schema).Returns(_gen2ExpandoObject);
    }

    protected static JsonObject ValueMap(int from, int to) => new()
    {
        ["status"] = new JsonObject
        {
            ["$mapValues"] = new JsonObject
            {
                ["source"] = "status",
                ["mappings"] = new JsonArray(new JsonObject { ["from"] = from, ["to"] = to })
            }
        }
    };

    protected static bool StatusIs(JsonObject content, int expected) =>
        content["status"] is JsonValue value && value.GetValue<int>() == expected;
}
