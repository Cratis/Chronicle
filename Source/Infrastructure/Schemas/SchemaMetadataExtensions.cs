// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
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
    static readonly ConditionalWeakTable<JsonSchema, Lazy<bool>> _protectionValidation = new();
    static readonly ConditionalWeakTable<JsonSchema, Lazy<bool>> _unprotectedValues = new();
    static readonly ConditionalWeakTable<JsonSchema, Lazy<bool>> _unprotectedContainers = new();
    static readonly SchemaMetadataCategory[] _categories = [SchemaMetadataCategory.Compliance, SchemaMetadataCategory.Security];
    static readonly string[] _dynamicSchemas = ["if", "then", "else", "not", "additionalProperties", "additionalItems", "contains", "unevaluatedProperties", "unevaluatedItems", "propertyNames", "contentSchema"];
    static readonly string[] _schemaMaps = ["patternProperties", "dependentSchemas", "dependencies"];

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

        var result = NestedSchemasAndSelf(schema).Any(node => node.Node.ContainsKey(KeyFor(category)));
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
        (includeMembers ? _unprotectedValues : _unprotectedContainers)
            .GetValue(schema, value => new Lazy<bool>(() => IsUnprotected(value, includeMembers))).Value;

    /// <summary>
    /// Rejects schema shapes whose protection cannot be completely resolved before a write or release.
    /// </summary>
    /// <param name="schema">The document schema.</param>
    /// <exception cref="UnresolvedSchemaProtection">A declaration cannot be resolved safely.</exception>
    public static void EnsureProtectionCanBeResolved(this JsonSchema schema) =>
        _ = _protectionValidation.GetValue(schema, value => new Lazy<bool>(() => ValidateProtection(value))).Value;

    /// <summary>
    /// Gets the schema key a category is stored under.
    /// </summary>
    /// <param name="category">The metadata category.</param>
    /// <returns>The extension data key.</returns>
    internal static string KeyFor(SchemaMetadataCategory category) =>
        category == SchemaMetadataCategory.Compliance ? ComplianceJsonSchemaExtensions.ComplianceKey : SecurityJsonSchemaExtensions.SecurityKey;

    static bool IsUnprotected(JsonSchema schema, bool includeMembers) =>
        NestedSchemasAndSelf(schema, includeMembers).All(node =>
            !node.Node.ContainsKey(ComplianceJsonSchemaExtensions.ComplianceKey) &&
            !node.Node.ContainsKey(SecurityJsonSchemaExtensions.SecurityKey) &&
            (!node.HasReference || node.Reference is not null) &&
            !node.Node.ContainsKey("$dynamicRef") && !node.Node.ContainsKey("$recursiveRef"));

    static bool ValidateProtection(JsonSchema schema)
    {
        if (!schema.HasSchemaMetadata()) return true;

        foreach (var declaration in NestedSchemasAndSelf(schema))
        {
            // An unrelated unprotected union or dictionary cannot change a protected member's handling.
            // Unresolved references remain unsafe in a protected document, even without a local marker.
            if (declaration.HasReference || !declaration.IsUnprotectedSchemaValue()) _ = declaration.ResolveComposition();
            if (DynamicSchemas(declaration).Any(child => !child.IsUnprotectedSchemaValue()) ||
                declaration.Node.ContainsKey("$dynamicRef") || declaration.Node.ContainsKey("$recursiveRef"))
            {
                throw new UnresolvedSchemaProtection("conditional or dynamic member declarations");
            }
            foreach (var category in _categories)
            {
                if (declaration.Node.TryGetPropertyValue(KeyFor(category), out var metadata) &&
                    (metadata is not JsonArray entries || entries.Count != declaration.GetSchemaMetadata(category).Count()))
                {
                    throw new UnresolvedSchemaProtection($"malformed {category} metadata");
                }
            }
        }

        return true;
    }

    static IEnumerable<JsonSchema> NestedSchemasAndSelf(JsonSchema schema, bool includeMembers = true)
    {
        var pending = new Stack<JsonSchema>();
        var references = new HashSet<(JsonSchema Root, string Reference)>();
        var dynamicRoots = new HashSet<JsonSchema>();
        pending.Push(schema);
        while (pending.TryPop(out var current))
        {
            yield return current;
            var members = includeMembers ? current.Properties.Values.Cast<JsonSchema>() : [];
            var dynamicSchemas = includeMembers ? DynamicSchemas(current) : [];
            foreach (var child in members.Concat(current.AllOf).Concat(current.AnyOf).Concat(current.OneOf).Concat(dynamicSchemas))
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

            // Dynamic anchors can live in otherwise unreachable, nested definitions. Revisit the owning
            // document once with definitions enabled; static reference tracking still terminates cycles.
            if ((current.Node.ContainsKey("$dynamicRef") || current.Node.ContainsKey("$recursiveRef")) && dynamicRoots.Add(current.Root))
            {
                pending.Push(current.Root);
            }
            if (dynamicRoots.Contains(current.Root))
            {
                foreach (var key in new[] { "$defs", "definitions" })
                {
                    if (current.Node[key] is not JsonObject definitions) continue;
                    foreach (var definition in definitions.Select(_ => _.Value).OfType<JsonObject>()) pending.Push(new JsonSchema(definition, current.Root));
                }
            }
        }
    }

    static IEnumerable<JsonSchema> DynamicSchemas(JsonSchema schema)
    {
        foreach (var key in _dynamicSchemas)
        {
            if (schema.Node[key] is JsonObject child) yield return new JsonSchema(child, schema.Root);
        }
        foreach (var key in _schemaMaps)
        {
            if (schema.Node[key] is not JsonObject map) continue;
            foreach (var child in map.Select(_ => _.Value).OfType<JsonObject>()) yield return new JsonSchema(child, schema.Root);
        }
        foreach (var key in new[] { "items", "prefixItems" })
        {
            if (schema.Node[key] is not JsonArray items) continue;
            foreach (var child in items.OfType<JsonObject>()) yield return new JsonSchema(child, schema.Root);
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
