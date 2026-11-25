// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchemaMetadataManager
{
    static bool CannotInspectReferencedType(JsonSchema schema, int depth) =>
        depth > 64 || schema.HasContextualReference;

    static bool HasUnstableClassification(JsonSchema schema, int depth) =>
        depth > 32 || schema.IsDynamic() || schema.HasContextualReference;

    static (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] ClassifyContextualReferences(JsonSchema.MetadataReach reach, SchemaMetadataCategory[] categories)
    {
        var metadata = new List<(SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)>();
        CollectMetadata(reach.Schemas[0], categories, metadata);
        foreach (var target in reach.Schemas.Skip(1).Where(SupportsContextualScalarMetadata))
        {
            CollectMetadata(target, categories, metadata);
        }
        return metadata.ToArray();
    }

    static bool SupportsContextualScalarMetadata(JsonSchema schema) => schema.Type == JsonObjectType.None
        ? CanInspectReferencedType(schema) && StrictJsonSchemaRelease.HasSupportedType(schema) &&
            (schema.ActualTypeSchema.Type & (JsonObjectType.Object | JsonObjectType.Array)) == JsonObjectType.None
        : (schema.Type & (JsonObjectType.Object | JsonObjectType.Array)) == JsonObjectType.None;

    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] Classify(JsonSchema schema, bool forComparison)
    {
        var categories = forComparison ? _comparisonCategories : _categories;
        var reach = schema.MetadataReferences;
        return reach.Contextual
            ? ClassifyContextualReferences(reach, categories)
            : ClassifyOrdinaryReferences(schema, categories);
    }
}
