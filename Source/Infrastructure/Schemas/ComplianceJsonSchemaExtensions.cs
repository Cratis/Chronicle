// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Linq;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/> for compliance.
/// </summary>
public static class ComplianceJsonSchemaExtensions
{
    /// <summary>
    /// The key of the compliance extension data.
    /// </summary>
    public const string ComplianceKey = "compliance";

    /// <summary>
    /// Ensure the compliance metadata is correct with correct types.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure.</param>
    public static void EnsureComplianceMetadata(this JsonSchema schema)
    {
        lock (schema)
        {
            ConvertComplianceIfNeeded(schema);

            if (schema.Properties.Count > 0)
            {
                foreach (var property in schema.Properties)
                {
                    ConvertComplianceIfNeeded(property.Value);
                }
            }
        }
    }

    /// <summary>
    /// Ensure the compliance metadata is correct with correct types.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to ensure.</param>
    public static void EnsureComplianceMetadata(this JsonSchemaProperty property) =>
        EnsureComplianceMetadata((JsonSchema)property);

    /// <summary>
    /// Get compliance metadata from schema. This is not recursive.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetComplianceMetadata(this JsonSchema schema)
    {
        lock (schema)
        {
            if (schema.ExtensionData is null) return [];
            if (!schema.ExtensionData.TryGetValue(ComplianceKey, out var value) || value is null) return [];

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
    /// Get compliance metadata from property. This is not recursive.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to get from.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetComplianceMetadata(this JsonSchemaProperty property) =>
        GetComplianceMetadata((JsonSchema)property);

    /// <summary>
    /// Check recursively if the schema has compliance metadata.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasComplianceMetadata(this JsonSchema schema)
    {
        var hasMetadata = schema.ExtensionData?.ContainsKey(ComplianceKey) ?? false;

        if (!hasMetadata && schema.Properties.Count > 0)
        {
            foreach (var property in schema.GetFlattenedProperties())
            {
                hasMetadata = property.HasComplianceMetadata();
                if (hasMetadata) break;
            }
        }

        return hasMetadata;
    }

    /// <summary>
    /// Check if the property has compliance metadata.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasComplianceMetadata(this JsonSchemaProperty property) =>
        HasComplianceMetadata((JsonSchema)property);

    static void ConvertComplianceIfNeeded(JsonSchema schema)
    {
        if (schema.ExtensionData is null) return;
        if (!schema.ExtensionData.TryGetValue(ComplianceKey, out var value) || value is null) return;

        // If it's already a typed list, nothing to do
        if (value is IEnumerable<ComplianceSchemaMetadata>) return;

        // Convert from JsonArray
        if (value is JsonArray arr)
        {
            schema.ExtensionData[ComplianceKey] = ParseFromJsonArray(arr);
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
            .Select(x => new ComplianceSchemaMetadata(Guid.Parse(x.MetadataTypeStr!), x.Details!))
            .ToList();
    }
}
