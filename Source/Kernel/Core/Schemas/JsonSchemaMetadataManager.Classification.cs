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

        // Discovery is deliberately broader than handler selection. Own annotations already select
        // this boundary; possible bindings must not introduce another operation on that value.
        if (metadata.Count > 0) return metadata.ToArray();
        var supportsUndeclaredScalar = reach.Schemas.Exists(schema => schema.Type != JsonObjectType.None) &&
            !reach.Schemas.Exists(schema => (schema.Type & (JsonObjectType.Object | JsonObjectType.Array)) != JsonObjectType.None);
        foreach (var target in reach.Schemas.Skip(1).Where(schema => SupportsContextualScalarMetadata(schema, supportsUndeclaredScalar)))
        {
            var candidate = new List<(SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)>();
            CollectMetadata(target, categories, candidate);
            foreach (var entry in candidate.Where(entry => entry.Category == SchemaMetadataCategory.Compliance && entry.Metadata.metadataType == "PII"))
            {
                if (!ContainsKey(metadata, entry)) metadata.Add(entry);
            }
        }
        return metadata.ToArray();
    }

    /// <summary>
    /// Determines whether a contextual target may supply scalar metadata. Recursive strict type restoration is not used
    /// to classify a graph: a declared scalar or an entirely scalar composition can supply PII; uncertainty never supplies an operation.
    /// </summary>
    /// <param name="schema">The candidate target schema.</param>
    /// <param name="supportsUndeclaredScalar">Whether an undeclared target may be treated as scalar.</param>
    /// <returns>True if the target may supply scalar metadata.</returns>
    static bool SupportsContextualScalarMetadata(JsonSchema schema, bool supportsUndeclaredScalar) =>
        (schema.Type != JsonObjectType.None || supportsUndeclaredScalar) &&
        (schema.Type & (JsonObjectType.Object | JsonObjectType.Array)) == JsonObjectType.None;

    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] Classify(JsonSchema schema, bool forComparison)
    {
        var categories = forComparison ? _comparisonCategories : _categories;
        var reach = schema.MetadataReferences;
        return reach.Contextual
            ? ClassifyContextualReferences(reach, categories)
            : ClassifyOrdinaryReferences(schema, categories);
    }
}
