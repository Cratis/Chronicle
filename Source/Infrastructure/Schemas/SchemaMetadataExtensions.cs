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
    /// Check recursively whether the schema carries metadata for a category that also matches a predicate over
    /// each entry's <see cref="ComplianceSchemaMetadata.metadataType"/>.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <param name="category">The <see cref="SchemaMetadataCategory"/> to check for.</param>
    /// <param name="predicate">Predicate over each candidate entry's metadata type.</param>
    /// <returns>True if a matching entry exists anywhere in the schema, false if not.</returns>
    /// <remarks>
    /// Unlike <see cref="HasSchemaMetadata(JsonSchema, SchemaMetadataCategory)"/>, this reads and tests every
    /// entry rather than stopping at "the extension key exists", and is not memoized - it exists for a caller
    /// that needs to distinguish between different metadata types within one category (for example, a
    /// namespace-/global-scoped security entry from a subject-scoped one), not merely whether the category is
    /// present at all.
    /// </remarks>
    public static bool HasSchemaMetadata(this JsonSchema schema, SchemaMetadataCategory category, Func<string, bool> predicate) =>
        HasMatchingSchemaMetadata(schema, category, predicate, [], 0);

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

    static bool HasMatchingSchemaMetadata(JsonSchema schema, SchemaMetadataCategory category, Func<string, bool> predicate, HashSet<JsonSchema> visited, int depth)
    {
        // Same traversal shape and termination guards as HasSchemaMetadata(JsonSchema, string, HashSet, int)
        // below - see its remarks - but this walk has to read and test each entry's metadata type rather than
        // stop at "the key exists", so it cannot share that method's cheap ContainsKey check or its cache.
        if (depth > MaxSchemaMetadataTraversalDepth || !visited.Add(schema))
        {
            return false;
        }

        return schema.GetSchemaMetadata(category).Any(metadata => predicate(metadata.metadataType)) ||
            NestedSchemas(schema).Any(child => HasMatchingSchemaMetadata(child, category, predicate, visited, depth + 1));
    }

    static bool HasSchemaMetadata(JsonSchema schema, string key, HashSet<JsonSchema> visited, int depth)
    {
        // References can create fresh wrappers at each level. At the depth limit protection is unknown,
        // not absent: keep the compliance walk enabled rather than allowing unexamined plaintext through.
        if (depth > MaxSchemaMetadataTraversalDepth)
        {
            return true;
        }
        if (!visited.Add(schema))
        {
            return false;
        }

        return (schema.ExtensionData?.ContainsKey(key) ?? false) ||
            NestedSchemas(schema).Any(child => HasSchemaMetadata(child, key, visited, depth + 1));
    }

    static IEnumerable<JsonSchema> NestedSchemas(JsonSchema schema)
    {
        // Resolving ActualTypeSchema first loses metadata on the wrapper, its local properties, or another
        // allOf member. Inspect the whole graph, including array items and nullable alternatives.
        foreach (var child in schema.Properties.Values.Cast<JsonSchema>().Concat(schema.AllOf).Concat(schema.AnyOf).Concat(schema.OneOf))
        {
            yield return child;
        }
        if (schema.Reference is { } reference)
        {
            yield return reference;
        }
        if (schema.Item is { } item)
        {
            yield return item;
        }
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
