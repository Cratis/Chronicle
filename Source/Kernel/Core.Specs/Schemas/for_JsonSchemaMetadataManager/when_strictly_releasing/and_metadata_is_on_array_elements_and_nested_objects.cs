// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_metadata_is_on_array_elements_and_nested_objects : given.real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": {
                "addresses": { "type": "array", "items": { "type": "object", "properties": {
                    "street": { "type": "string" }, "number": { "type": "integer" }
                } } },
                "emails": { "type": "array", "items": { "type": "string" } },
                "nested": { "type": "object", "properties": { "secret": { "type": "boolean" }, "visible": { "type": "string" } } },
                "matrix": { "type": "array", "items": { "type": "array", "items": { "type": "integer" } } }
            } }
            """);
        Mark(_schema.Properties["addresses"].Item!);
        Mark(_schema.Properties["emails"].Item!);
        Mark(_schema.Properties["nested"].Properties["secret"]);
        Mark(_schema.Properties["matrix"].Item!.Item!);
        _plaintext = JsonNode.Parse("""{"addresses":[{"street":"Private Lane","number":42}],"emails":["a@example.com","b@example.com"],"nested":{"secret":true,"visible":"public"},"matrix":[[1,2],[3,4]]} """)!.AsObject();
        await Protect();
    }

    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_mirror_the_apply_traversal_and_restore_all_values() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
    [Fact] void should_encrypt_object_element_members_not_the_object() => _protected["addresses"]![0]!.GetValueKind().ShouldEqual(JsonValueKind.Object);
    [Fact] void should_encrypt_the_integer_member() => _protected["addresses"]![0]!["number"]!.GetValueKind().ShouldEqual(JsonValueKind.String);
}
