// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Schemas;

public partial class JsonSchemaMetadataManager
{
    static bool CannotInspectReferencedType(int depth) =>
        depth > 64;

    static bool HasUnstableClassification(JsonSchema schema, int depth) =>
        depth > 32 || schema.IsDynamic() || schema.HasContextualReference;

    static (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] ClassifyContextualReferences(JsonSchema.MetadataReach reach, SchemaMetadataCategory[] categories, (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] ordinary)
    {
        var metadata = ordinary.ToList();

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

    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] ForApply(
        MetadataWalk walk,
        JsonSchema schema,
        (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] metadata,
        string path)
    {
        // Comparison reuses the apply traversal, but must never drop a protected boundary at the write floor.
        if (walk.Comparing || walk.ActionName != SchemaMetadataActionFailed.ApplyAction || metadata.Length == 0 || !schema.MetadataReferences.Contextual)
        {
            return metadata;
        }

        var ordinary = ClassifyOrdinaryReferences(schema, _categories).ToList();
        return metadata.Where(entry =>
        {
            if (ContainsKey(ordinary, entry) || !_propertyValueHandlers.TryGetValue((entry.Category, entry.Metadata.metadataType), out var handler) ||
                handler.SupportsTypedValues || walk.LegacyTypes.HasSupportedType(schema))
            {
                return true;
            }

            logger.TypedProtectedValueFloorRequired(path, ClusterReleaseBoundary.TypedProtectedValues.ToString());
            return false;
        }).ToArray();
    }

    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] Classify(JsonSchema schema, bool forComparison)
    {
        var categories = forComparison ? _comparisonCategories : _categories;
        var reach = schema.MetadataReferences;
        return reach.Contextual
            ? ClassifyContextualReferences(reach, categories, ClassifyOrdinaryReferences(schema, categories))
            : ClassifyOrdinaryReferences(schema, categories);
    }
}
