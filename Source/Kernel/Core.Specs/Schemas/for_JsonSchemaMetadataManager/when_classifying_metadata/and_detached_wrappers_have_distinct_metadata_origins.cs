// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_detached_wrappers_have_distinct_metadata_origins : given.real_pii_protection
{
    JsonSchemaProperty _property;
    bool _compliance;
    bool _security;
    bool _pii;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _metadata;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _containerMetadata;

    void Establish()
    {
        const string resource = "https://schemas.example/programmatic";
        _schema = new JsonSchema(new JsonObject { ["$id"] = resource }) { Type = JsonObjectType.Object };
        _property = new JsonSchemaProperty("value", new JsonObject(), _schema)
        {
            Type = JsonObjectType.String,
            ExtensionData = new Dictionary<string, object?>
            {
                ["compliance"] = new List<ComplianceSchemaMetadata> { new("PII", string.Empty) }
            }
        };
        _schema.Properties["value"] = _property;
        var independentRoot = new JsonSchema(new JsonObject { ["$id"] = resource });
        _schema.Properties["security"] = new JsonSchemaProperty("security", new JsonObject(), independentRoot)
        {
            Type = JsonObjectType.String,
            ExtensionData = new Dictionary<string, object?>
            {
                ["security"] = new List<ComplianceSchemaMetadata> { new("Secret", string.Empty) }
            }
        };
    }

    void Because()
    {
        _compliance = _schema.HasComplianceMetadata();
        _security = _schema.HasSecurityMetadata();
        _pii = _schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance, type => type == "PII");
        _metadata = _manager.MetadataFor(_property, forComparison: false);
        _containerMetadata = _manager.MetadataFor(_schema, forComparison: false);
    }

    [Fact] void should_visit_a_parent_bound_property_constructed_before_attachment() => _compliance.ShouldBeTrue();
    [Fact] void should_visit_an_independent_tree_with_the_same_resource_uri() => _security.ShouldBeTrue();
    [Fact] void should_find_the_real_metadata_type() => _pii.ShouldBeTrue();
    [Fact] void should_retain_the_scalar_handler_metadata() => _metadata.Length.ShouldEqual(1);
    [Fact] void should_not_promote_child_metadata_to_a_container_handler() => _containerMetadata.Length.ShouldEqual(0);
}
