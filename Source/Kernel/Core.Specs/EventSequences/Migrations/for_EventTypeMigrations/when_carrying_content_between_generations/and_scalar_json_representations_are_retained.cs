// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Geospatial;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_scalar_json_representations_are_retained : given.a_schema_filtered_chain
{
    void Establish()
    {
        _source = JsonNode.Parse("""
            {"state":1,"amount":1234567890.123456789012345678,"counter":18446744073709551615,
             "at":"2026-03-04T05:06:07.1234567+05:30","plain":"2026-03-04T05:06:07.1234567Z",
             "duration":"01:02:03","id":"A0B1C2D3-E4F5-4678-9012-3456789ABCDE","blob":"AAECAw==",
             "point":{"type":"Point","coordinates":[1.5,2.5]}}
            """)!.AsObject();
        var schema = Schema("""
            {"state":{"type":"integer","enum":[0,1],"x-enumNames":["New","Done"]},
             "amount":{"type":"number","format":"decimal"},"counter":{"type":"integer","format":"uint64"},
             "at":{"type":"string","format":"date-time-offset"},"plain":{"type":"string","format":"date-time"},
             "duration":{"type":"string","format":"duration"},"id":{"type":"string","format":"guid"},
             "blob":{"type":"string","format":"byte-array"},
             "point":{"type":"object","format":"point","properties":{"type":{"type":"string"}}},
             "copied":{"type":["string","null"]}}
            """);
        _definition = _definition with
        {
            Generations = [new(1, schema), new(2, schema), new(3, schema)],
            Migrations = [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject { ["copied"] = new JsonObject { ["$rename"] = "at" } }, new JsonObject())]
        };
    }

    async Task Because() => _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, new(), Converted, Migrating);

    [Fact] void should_keep_every_original_json_value_at_the_second_edge() => _source.All(property => JsonNode.DeepEquals(property.Value, _inputs[1][property.Key])).ShouldBeTrue();
    [Fact] void should_keep_the_enum_numeric() => _inputs[1]["state"]!.GetValue<int>().ShouldEqual(1);
    [Fact] void should_preserve_the_formatted_timestamp_for_expressions() => Members(_result[3])["copied"].ShouldEqual("2026-03-04T05:06:07.1234567+05:30");
    [Fact] void should_keep_decimal_precision() => Members(_result[3])["amount"].ShouldEqual(1234567890.123456789012345678m);
    [Fact] void should_keep_the_unsigned_range() => Members(_result[3])["counter"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_keep_formatted_object_values_atomic() => Members(_result[3])["point"].ShouldBeOfExactType<Point>();
}
