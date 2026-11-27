// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_cyclic_contextual_compositions_have_no_handler : given.real_pii_protection
{
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _metadata;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "properties":{"value":{"$dynamicRef":"#value"}},
          "$defs":{"base":{"$dynamicAnchor":"value","allOf":[{"$ref":"#/$defs/base"}],"anyOf":[{"$ref":"#/$defs/base"}],"oneOf":[{"$ref":"#/$defs/base"}]}}
        }
        """);

    void Because() => _metadata = _manager.MetadataFor(_schema.Properties["value"], forComparison: true);

    [Fact] void should_terminate_without_strict_recursive_type_inference() => _metadata.Length.ShouldEqual(0);
}
