// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_protected_values_have_single_types : given.real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": {
                "text": { "type": "string" }, "number": { "type": "number" },
                "integer": { "type": "integer" }, "boolean": { "type": "boolean" },
                "array": { "type": "array", "items": { "type": "integer" } },
                "object": { "type": "object", "properties": { "name": { "type": "string" } } },
                "nullable": { "oneOf": [{ "type": "null" }, { "type": "integer" }] }
            } }
            """);
        foreach (var property in _schema.Properties.Values)
        {
            Mark(property);
        }

        _plaintext = JsonNode.Parse("""{"text":"42","number":42.5,"integer":42,"boolean":true,"array":[1,2],"object":{"name":"private"},"nullable":42} """)!.AsObject();
        await Protect();
    }

    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_restore_the_declared_types_without_losing_content() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
    [Fact] void should_not_treat_numeric_text_as_a_number() => _result["text"]!.GetValue<string>().ShouldEqual("42");
    [Fact] void should_have_encrypted_the_container_as_a_whole() => _protected["array"]!.GetValueKind().ShouldEqual(System.Text.Json.JsonValueKind.String);
}
