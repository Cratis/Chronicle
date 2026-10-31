// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_classified_for_comparison_after_action : given.real_pii_protection
{
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _forAction;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _forComparison;

    void Establish() => _schema = new JsonSchema(JsonNode.Parse("""{"type":"string","security":[{"metadataType":"Confidential","details":""}]}""")!.AsObject());

    void Because()
    {
        _forAction = _manager.MetadataFor(_schema, forComparison: false);
        _forComparison = _manager.MetadataFor(_schema, forComparison: true);
    }

    [Fact] void should_exclude_security_for_action() => _forAction.Length.ShouldEqual(0);
    [Fact] void should_include_security_for_comparison() => _forComparison.Select(_ => _.Category).ShouldContainOnly(SchemaMetadataCategory.Security);
}
