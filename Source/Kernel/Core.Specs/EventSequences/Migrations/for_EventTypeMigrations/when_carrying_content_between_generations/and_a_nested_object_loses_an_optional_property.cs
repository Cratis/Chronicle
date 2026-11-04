// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_a_nested_object_loses_an_optional_property : given.a_schema_filtered_chain
{
    void Establish()
    {
        _source = JsonNode.Parse("""{"details":{"id":"1","status":"legacy"}}""")!.AsObject();
        var full = Schema("""{"details":{"type":"object","properties":{"id":{"type":"string"},"status":{"type":["string","null"]}}}}""");
        var intermediate = Schema("""{"details":{"type":"object","properties":{"id":{"type":"string"}}}}""");
        _definition = _definition with
        {
            Generations = [new(1, full), new(2, intermediate), new(3, full)],
            Migrations = [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], Default("details.status"), new JsonObject())]
        };
    }

    async Task Because() => _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, new(), Converted, Migrating);

    [Fact] void should_remove_the_nested_property_before_the_next_edge() => _inputs[1]["details"]!.AsObject().ContainsKey("status").ShouldBeFalse();
    [Fact] void should_apply_the_nested_default() => Members((ExpandoObject)Members(_result[3])["details"]!)["status"].ShouldEqual("pending");
    [Fact] void should_keep_the_nested_sibling() => _inputs[1]["details"]!["id"]!.GetValue<string>().ShouldEqual("1");
}
