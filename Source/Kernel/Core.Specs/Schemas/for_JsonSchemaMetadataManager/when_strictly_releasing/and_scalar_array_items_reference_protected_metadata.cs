// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_strictly_releasing;

public class and_scalar_array_items_reference_protected_metadata : given.real_pii_protection
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"emails":{"type":"array","items":{"$ref":"#/definitions/email"}}},
             "definitions":{"email":{"type":"string"}}}
            """);
        Mark(_schema.Properties["emails"].Item!.ActualSchema);
        _plaintext = JsonNode.Parse("""{"emails":["private@example.com",""]}""")!.AsObject();
        await Protect();
    }

    async Task Because() => _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);

    [Fact] void should_release_using_the_referenced_item_metadata() => JsonNode.DeepEquals(_result, _plaintext).ShouldBeTrue();
}
