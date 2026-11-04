// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_two_schema_instances_share_a_shape : given.real_pii_protection
{
    JsonObject _unmarkedInput;
    JsonObject _unmarkedResult;
    JsonObject _markedInput;
    JsonObject _markedResult;

    async Task Because()
    {
        var unmarked = SchemaWith(marked: false);
        _unmarkedInput = JsonNode.Parse("""{"value":"Ada"}""")!.AsObject();
        _manager.MetadataFor(unmarked.Properties["value"], forComparison: false);
        _unmarkedResult = await _manager.Apply("store", "tenant", unmarked, "subject", _unmarkedInput);

        var marked = SchemaWith(marked: true);
        _markedInput = JsonNode.Parse("""{"value":"Ada"}""")!.AsObject();
        _markedResult = await _manager.Apply("store", "tenant", marked, "subject", _markedInput);
    }

    [Fact] void should_leave_the_unmarked_instance_unprotected() => JsonNode.DeepEquals(_unmarkedInput, _unmarkedResult).ShouldBeTrue();
    [Fact] void should_protect_the_marked_instance() => JsonNode.DeepEquals(_markedInput, _markedResult).ShouldBeFalse();

    static JsonSchema SchemaWith(bool marked)
    {
        var root = JsonNode.Parse("""{"type":"object","properties":{"value":{"$ref":"#/$defs/definition"}},"$defs":{"definition":{"type":"string"}}}""")!.AsObject();
        if (marked)
        {
            root["$defs"]!["definition"]!["compliance"] = JsonNode.Parse("""[{"metadataType":"PII","details":""}]""");
        }

        return new JsonSchema(root);
    }
}
