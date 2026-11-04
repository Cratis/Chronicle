// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_an_object_list_loses_optional_properties : given.a_schema_filtered_chain
{
    void Establish()
    {
        _source = JsonNode.Parse("""{"items":[{"id":"first","status":"legacy"},null,{"id":"last","status":"legacy"}]}""")!.AsObject();
        var full = Schema("""{"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"status":{"type":["string","null"]}}}}}""");
        var intermediate = Schema("""{"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"}}}}}""");
        _definition = _definition with
        {
            Generations = [new(1, full), new(2, intermediate), new(3, full)],
            Migrations = [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())]
        };
    }

    async Task Because() => _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, new(), Converted, Migrating);

    [Fact] void should_remove_optional_properties_from_every_object() => _inputs[1]["items"]!.AsArray().OfType<JsonObject>().All(item => !item.ContainsKey("status")).ShouldBeTrue();
    [Fact] void should_keep_order_siblings_and_null_items() => _inputs[1]["items"]!.ToJsonString().ShouldEqual("""[{"id":"first"},null,{"id":"last"}]""");
    [Fact] void should_not_modify_the_original_items() => _source["items"]![0]!.AsObject().ContainsKey("status").ShouldBeTrue();
}
