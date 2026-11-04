// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_open_content_is_retained : given.a_schema_filtered_chain
{
    void Establish()
    {
        _source = JsonNode.Parse("""{"open":{"arbitrary":{"value":1}},"map":{"key":{"value":"kept"}},"dynamic":{"anything":true},"untyped":{"another":[1,2]},"rows":[{"arbitrary":1}]}""")!.AsObject();
        var schema = Schema("""{"open":{"type":"object"},"map":{"type":"object","additionalProperties":{"type":"object"}},"dynamic":{"type":"object","format":"dynamic"},"untyped":{},"rows":{"type":"array","items":{}}}""");
        _definition = _definition with
        {
            Generations = [new(1, schema), new(2, schema), new(3, schema)],
            Migrations = [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())]
        };
    }

    async Task Because() => _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, new(), Converted, Migrating);

    [Fact] void should_preserve_open_and_dictionary_content() => JsonNode.DeepEquals(_source, _inputs[1]).ShouldBeTrue();
}
