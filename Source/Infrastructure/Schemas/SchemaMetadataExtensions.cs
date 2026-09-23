// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/> for reading and writing schema metadata by
/// <see cref="SchemaMetadataCategory"/> - the generalized mechanism <see cref="ComplianceJsonSchemaExtensions"/>
/// and the security-specific call sites both build on.
/// </summary>
/// <remarks>
/// A schema node can carry metadata for more than one, unrelated reason - see <see cref="SchemaMetadataCategory"/>
/// for why compliance and security are kept apart rather than sharing one bucket. Each category is stored under
/// its own key in the schema's extension data, so a schema can carry both without either reader having to
/// disambiguate entries that were never theirs. What is shared is the mechanics: normalizing raw JSON into typed
/// <see cref="ComplianceSchemaMetadata"/> entries, the recursive has-metadata walk with its cycle and depth guards,
/// and the per-category memoization. None of that is specific to compliance or to security - it is generic JSON
/// schema metadata handling, which is why it lives here rather than duplicated once per category.
/// </remarks>
public static class SchemaMetadataExtensions
{
    const int MaxSchemaMetadataTraversalDepth = 64;

    /// <summary>
    /// Ensure the schema metadata for a given category on the schema node itself is typed rather than raw JSON.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure.</param>
    /// <param name="category">The <see cref="SchemaMetadataCategory"/> to ensure.</param>
    /// <remarks>
    /// This normalizes only the node it is given, and deliberately so. Schema metadata can sit at any depth —
    /// inside a value object, inside an array's item schema — but every reader goes through
    /// <see cref="GetSchemaMetadata(JsonSchema, SchemaMetadataCategory)"/>, which accepts the raw <see cref="JsonArray"/>
    /// form just as readily as the typed one, for whichever node it is handed. Walking the whole schema here would
    /// deep clone every nested node on each stored-schema load and change nothing about what those readers see.
    /// </remarks>
    public static void EnsureSchemaMetadata(this JsonSchema schema, SchemaMetadataCategory category)
    {
        lock (schema)
        {
            ConvertIfNeeded(schema, KeyFor(category));
        }
    }

    /// <summary>
    /// Get schema metadata for a given category from a schema. This is not recursive.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <param name="category">The <see cref="SchemaMetadataCategory"/> to get.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetSchemaMetadata(this JsonSchema schema, SchemaMetadataCategory category)
    {
        lock (schema)
        {
            var key = KeyFor(category);
            if (schema.ExtensionData is null) return [];
            if (!schema.ExtensionData.TryGetValue(key, out var value) || value is null) return [];

            // Already typed
            if (value is IEnumerable<ComplianceSchemaMetadata> typedMetadata)
                return typedMetadata;

            // Raw from JSON deserialization - JsonArray
            if (value is JsonArray arr)
                return ParseFromJsonArray(arr);

            return [];
        }
    }

    /// <summary>
    /// Get schema metadata for a given category from a property. This is not recursive.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to get from.</param>
    /// <param name="category">The <see cref="SchemaMetadataCategory"/> to get.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetSchemaMetadata(this JsonSchemaProperty property, SchemaMetadataCategory category) =>
        GetSchemaMetadata((JsonSchema)property, category);

    /// <summary>
    /// Check recursively if the schema has metadata for a given category.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <param name="category">The <see cref="SchemaMetadataCategory"/> to check for.</param>
    /// <returns>True if it has, false if not.</returns>
    /// <remarks>
    /// The recursive walk is run once per schema instance, per category, and its result memoized, because the
    /// answer is invoked for every appended and read event and a schema is effectively immutable once built.
    /// </remarks>
    public static bool HasSchemaMetadata(this JsonSchema schema, SchemaMetadataCategory category)
    {
        var cached = category == SchemaMetadataCategory.Compliance ? schema.CachedHasComplianceMetadata : schema.CachedHasSecurityMetadata;
        if (cached is { } value)
        {
            return value;
        }

        var result = HasSchemaMetadata(schema, KeyFor(category), [], 0);
        if (category == SchemaMetadataCategory.Compliance)
        {
            schema.CachedHasComplianceMetadata = result;
        }
        else
        {
            schema.CachedHasSecurityMetadata = result;
        }

        return result;
    }

    /// <summary>
    /// Check if the property has metadata for a given category.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to check.</param>
    /// <param name="category">The <see cref="SchemaMetadataCategory"/> to check for.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasSchemaMetadata(this JsonSchemaProperty property, SchemaMetadataCategory category) =>
        HasSchemaMetadata((JsonSchema)property, category);

    /// <summary>
    /// Check recursively whether the schema has metadata for any known category.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if it has metadata for at least one category, false if not.</returns>
    /// <remarks>
    /// Used where the caller's next step is category-agnostic - typically "is there any reason to walk this
    /// document at all" - and does not itself need to know which category matched. Each category's own answer is
    /// still independently memoized via <see cref="HasSchemaMetadata(JsonSchema, SchemaMetadataCategory)"/>.
    /// </remarks>
    public static bool HasSchemaMetadata(this JsonSchema schema) =>
        schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance) || schema.HasSchemaMetadata(SchemaMetadataCategory.Security);

    /// <summary>
    /// Gets the schema key a given <see cref="SchemaMetadataCategory"/> is stored under.
    /// </summary>
    /// <param name="category">The <see cref="SchemaMetadataCategory"/> to get the key for.</param>
    /// <returns>The extension data key.</returns>
    internal static string KeyFor(SchemaMetadataCategory category) =>
        category == SchemaMetadataCategory.Compliance ? ComplianceJsonSchemaExtensions.ComplianceKey : SecurityJsonSchemaExtensions.SecurityKey;

    static bool HasSchemaMetadata(JsonSchema schema, string key, HashSet<JsonSchema> visited, int depth)
    {
        // Walking a schema graph to look for metadata has to contend with recursive read models (e.g. a child
        // collection of the model's own type). Two termination guards:
        //   1. A visited set keyed on the resolved ActualSchema, which recognises a cycle whenever
        //      references resolve back to one shared definition object.
        //   2. A depth bound, because NJsonSchema does not guarantee a stable identity for schemas
        //      reached through $ref/Item — its accessors can hand back a fresh wrapper on each
        //      access, so (1) alone is not enough for every shape. The bound guarantees termination
        //      regardless of identity, and does not cause false negatives: schema metadata is
        //      shallow, so anything real is found long before this depth.
        var actual = schema.ActualSchema;
        if (depth > MaxSchemaMetadataTraversalDepth || !visited.Add(actual))
        {
            return false;
        }

        var hasMetadata = actual.ExtensionData?.ContainsKey(key) ?? false;

        if (!hasMetadata && actual.Properties.Count > 0)
        {
            foreach (var property in actual.GetFlattenedProperties())
            {
                hasMetadata = HasSchemaMetadata(property, key, visited, depth + 1);
                if (hasMetadata) break;
            }
        }

        // Metadata can live on an array's element type — e.g. IReadOnlyList<Email> where Email is a [PII]
        // concept, or a list of objects carrying [PII]/[Encrypted] members. Without descending into the item
        // schema, list-valued metadata is missed entirely and stored in the clear.
        if (!hasMetadata && actual.Item is not null)
        {
            hasMetadata = HasSchemaMetadata(actual.Item, key, visited, depth + 1);
        }

        return hasMetadata;
    }

    static void ConvertIfNeeded(JsonSchema schema, string key)
    {
        if (schema.ExtensionData is null) return;
        if (!schema.ExtensionData.TryGetValue(key, out var value) || value is null) return;

        // If it's already a typed list, nothing to do
        if (value is IEnumerable<ComplianceSchemaMetadata>) return;

        // Convert from JsonArray
        if (value is JsonArray arr)
        {
            schema.ExtensionData[key] = ParseFromJsonArray(arr);
        }
    }

    static List<ComplianceSchemaMetadata> ParseFromJsonArray(JsonArray arr)
    {
        return arr
            .OfType<JsonObject>()
            .Select(obj => new
            {
                MetadataTypeStr = obj[nameof(ComplianceSchemaMetadata.metadataType)]?.GetValue<string>(),
                Details = obj[nameof(ComplianceSchemaMetadata.details)]?.GetValue<string>()
            })
            .Where(x => x.MetadataTypeStr is not null && x.Details is not null)
            .Select(x => new ComplianceSchemaMetadata(x.MetadataTypeStr!, x.Details!))
            .ToList();
    }
}
