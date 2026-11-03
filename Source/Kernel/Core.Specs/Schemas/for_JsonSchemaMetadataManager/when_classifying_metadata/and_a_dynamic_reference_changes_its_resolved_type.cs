// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_a_dynamic_reference_changes_its_resolved_type : given.real_pii_protection
{
    JsonSchema _property;
    JsonSchema _dynamic;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _first;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _second;

    void Establish()
    {
        _schema = new JsonSchema(JsonNode.Parse("""{"type":"object","properties":{"value":{"$ref":"#/$defs/value"}},"$defs":{"value":{"type":"string","format":"dynamic"}}}""")!.AsObject());
        _property = _schema.Properties["value"];
        _dynamic = _property.Reference!;
        Mark(_dynamic);
    }

    void Because()
    {
        _first = _manager.MetadataFor(_property, forComparison: false);
        _dynamic.Type = JsonObjectType.Object;
        _second = _manager.MetadataFor(_property, forComparison: false);
    }

    [Fact] void should_initially_include_the_scalar_marker() => _first.Length.ShouldEqual(1);
    [Fact] void should_not_reuse_the_scalar_classification_for_the_container() => _second.Length.ShouldEqual(0);
}
