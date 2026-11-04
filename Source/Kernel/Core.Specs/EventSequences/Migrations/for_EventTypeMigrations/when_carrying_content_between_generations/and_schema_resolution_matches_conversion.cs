// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_schema_resolution_matches_conversion : given.a_schema_filtered_chain
{
    void Establish()
    {
        _source = JsonNode.Parse("""{"inherited":"keep","details":{"id":"1","status":"legacy"},"rows":[{"id":"2","status":"legacy"}],"choice":{"id":"3","status":"legacy"}}""")!.AsObject();
        var schema = JsonSchema.FromJson("""
            {"type":"object","$defs":{
                "Base":{"type":"object","properties":{"inherited":{"type":"string"}}},
                "Detail":{"type":"object","properties":{"id":{"type":"string"}}}},
             "allOf":[{"$ref":"#/$defs/Base"}],
             "properties":{"details":{"$ref":"#/$defs/Detail"},
                "rows":{"type":"array","items":{"$ref":"#/$defs/Detail"}},
                "choice":{"anyOf":[{"type":"null"},{"$ref":"#/$defs/Detail"}]}}}
            """);
        _definition = _definition with
        {
            Generations = [new(1, schema), new(2, schema), new(3, schema)],
            Migrations = [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())]
        };
    }

    async Task Because() => _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, new(), Converted, Migrating);

    [Fact] void should_keep_all_of_properties() => _inputs[1]["inherited"]!.GetValue<string>().ShouldEqual("keep");
    [Fact] void should_resolve_object_references() => _inputs[1]["details"]!.AsObject().ContainsKey("status").ShouldBeFalse();
    [Fact] void should_resolve_array_item_references() => _inputs[1]["rows"]![0]!.AsObject().ContainsKey("status").ShouldBeFalse();
    [Fact] void should_resolve_nullable_any_of() => _inputs[1]["choice"]!.AsObject().ContainsKey("status").ShouldBeFalse();
}
