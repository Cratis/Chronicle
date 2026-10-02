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
/// <see cref="ComplianceSchemaMetadata"/> entries, the graph walk with reference-cycle detection,
/// and the per-category memoization. None of that is specific to compliance or to security - it is generic JSON
/// schema metadata handling, which is why it lives here rather than duplicated once per category.
/// </remarks>
public static class SchemaMetadataExtensions
{
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

        var result = NestedSchemasAndSelf(schema).Any(node => node.ExtensionData?.ContainsKey(KeyFor(category)) == true);
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
        NestedSchemasAndSelf(schema).Any(node => node.GetSchemaMetadata(category).Any(metadata => predicate(metadata.metadataType)));

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
    /// Determines whether a schema is fully resolved and declares no protection on this value.
    /// </summary>
    /// <param name="schema">The schema to inspect.</param>
    /// <param name="includeMembers">Whether nested properties and collection elements are part of the value being restored.</param>
    /// <returns>True only when restoring the value cannot bypass declared or unresolved protection.</returns>
    public static bool IsUnprotectedSchemaValue(this JsonSchema schema, bool includeMembers = true) =>
        NestedSchemasAndSelf(schema, includeMembers).All(node =>
            node.ExtensionData?.ContainsKey(ComplianceJsonSchemaExtensions.ComplianceKey) != true &&
            node.ExtensionData?.ContainsKey(SecurityJsonSchemaExtensions.SecurityKey) != true &&
            (!node.HasReference || node.Reference is not null));

    /// <summary>
    /// Rejects schema shapes whose protection cannot be completely resolved before a write or release.
    /// </summary>
    /// <param name="schema">The document schema.</param>
    /// <exception cref="UnresolvedSchemaProtection">A declaration cannot be resolved safely.</exception>
    public static void EnsureProtectionCanBeResolved(this JsonSchema schema)
    {
        foreach (var declaration in NestedSchemasAndSelf(schema))
        {
            _ = declaration.ResolveComposition();
            if (declaration.Node.Any(_ => new[] { "if", "then", "else", "not", "patternProperties", "dependentSchemas", "dependencies", "prefixItems", "contains", "unevaluatedProperties", "unevaluatedItems", "propertyNames", "$dynamicRef", "$recursiveRef", "contentSchema" }.Contains(_.Key, StringComparer.Ordinal) ||
                ((_.Key == "additionalProperties" || _.Key == "additionalItems") && _.Value is JsonObject) ||
                (_.Key == "items" && _.Value is JsonArray)))
            {
                throw new UnresolvedSchemaProtection("conditional or dynamic member declarations");
            }
            foreach (var category in new[] { SchemaMetadataCategory.Compliance, SchemaMetadataCategory.Security })
            {
                if (declaration.Node.TryGetPropertyValue(KeyFor(category), out var metadata) &&
                    (metadata is not JsonArray entries || entries.Count != declaration.GetSchemaMetadata(category).Count()))
                {
                    throw new UnresolvedSchemaProtection($"malformed {category} metadata");
                }
            }
        }
    }

    /// <summary>
    /// Gets the schema key a category is stored under.
    /// </summary>
    /// <param name="category">The metadata category.</param>
    /// <returns>The extension data key.</returns>
    internal static string KeyFor(SchemaMetadataCategory category) =>
        category == SchemaMetadataCategory.Compliance ? ComplianceJsonSchemaExtensions.ComplianceKey : SecurityJsonSchemaExtensions.SecurityKey;

    static IEnumerable<JsonSchema> NestedSchemasAndSelf(JsonSchema schema, bool includeMembers = true)
    {
        var pending = new Stack<JsonSchema>();
        var references = new HashSet<(JsonSchema Root, string Reference)>();
        pending.Push(schema);
        while (pending.TryPop(out var current))
        {
            yield return current;
            var members = includeMembers ? current.Properties.Values.Cast<JsonSchema>() : [];
            foreach (var child in members.Concat(current.AllOf).Concat(current.AnyOf).Concat(current.OneOf))
            {
                pending.Push(child);
            }

            // Resolving a reference creates a fresh wrapper. Track the reference in its owning document,
            // not wrapper identity or depth: a cycle says nothing about whether metadata exists.
            if (current.Node["$ref"]?.GetValue<string>() is { } reference &&
                references.Add((current.Root, reference)) && current.Reference is { } target)
            {
                pending.Push(target);
            }
            if (includeMembers && current.Item is { } item)
            {
                pending.Push(item);
            }
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
