// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_the_referenced_definition_is_a_protected_scalar : given.real_pii_protection
{
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _scalar;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _object;

    void Because()
    {
        _scalar = _manager.MetadataFor(PropertyReferencing("""{"type":"string","compliance":[{"metadataType":"PII","details":""}]}"""), forComparison: false);
        _object = _manager.MetadataFor(PropertyReferencing("""{"type":"object","compliance":[{"metadataType":"PII","details":""}],"properties":{"city":{"type":"string"}}}"""), forComparison: false);
    }

    [Fact] void should_include_the_referenced_metadata() => _scalar.Length.ShouldEqual(1);
    [Fact] void should_not_include_the_referenced_metadata_of_a_container() => _object.Length.ShouldEqual(0);

    static JsonSchema PropertyReferencing(string definition)
    {
        var root = JsonNode.Parse("""{"type":"object","properties":{"value":{"$ref":"#/$defs/definition"}},"$defs":{}}""")!.AsObject();
        root["$defs"]!["definition"] = JsonNode.Parse(definition);
        return new JsonSchema(root).Properties["value"];
    }
}
