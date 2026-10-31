// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_the_same_schema_is_classified_twice : given.real_pii_protection
{
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _first;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _second;

    void Establish()
    {
        _schema = new JsonSchema { Type = JsonObjectType.String };
        Mark(_schema);
    }

    void Because()
    {
        _first = _manager.MetadataFor(_schema, forComparison: false);
        _second = _manager.MetadataFor(_schema, forComparison: false);
    }

    [Fact] void should_return_the_same_classification() => _second.ShouldBeSame(_first);
    [Fact] void should_classify_the_marker() => _first.Length.ShouldEqual(1);
}
