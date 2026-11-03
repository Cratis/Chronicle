// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_null_and_case_selection_match_conversion : given.a_schema_filtered_chain
{
    void Establish()
    {
        _source = JsonNode.Parse("""{"NAME":"first","Name":"second","name":null,"mIxEd":"case value","explicit":null,"unknown":"drop"}""")!.AsObject();
        var schema = Schema("""{"name":{"type":["string","null"]},"mixed":{"type":"string"},"explicit":{"type":["string","null"]},"missing":{"type":"integer"}}""");
        _definition = _definition with
        {
            Generations = [new(1, schema), new(2, schema), new(3, schema)],
            Migrations = [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())]
        };
    }

    async Task Because() => _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, new(), Converted, Migrating);

    [Fact] void should_match_the_converter_selection_for_an_exact_null_collision() => Members(_result[1])["name"].ShouldEqual("first");
    [Fact] void should_carry_the_same_collision_winner() => _inputs[0]["name"]!.GetValue<string>().ShouldEqual("first");
    [Fact] void should_use_canonical_schema_casing() => _inputs[0]["mixed"]!.GetValue<string>().ShouldEqual("case value");
    [Fact] void should_keep_explicit_null_members() => _inputs[0].ContainsKey("explicit").ShouldBeTrue();
    [Fact] void should_not_materialize_converter_defaults_in_json() => _inputs[0].ContainsKey("missing").ShouldBeFalse();
    [Fact] void should_filter_unknown_properties() => _inputs[0].ContainsKey("unknown").ShouldBeFalse();
    [Fact] void should_not_carry_duplicate_case_variants() => _inputs[0].ContainsKey("NAME").ShouldBeFalse();
}
