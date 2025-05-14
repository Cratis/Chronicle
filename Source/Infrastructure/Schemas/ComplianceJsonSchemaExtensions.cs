// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

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
            if ((schema.ExtensionData?.ContainsKey(ComplianceKey) ?? false) &&
                schema.ExtensionData[ComplianceKey] is object[] complianceObjects)
            {
                var metadata = new List<ComplianceSchemaMetadata>();
                foreach (var complianceObject in complianceObjects)
                {
                    if (complianceObject is Dictionary<string, object> properties)
                    {
                        var metadataType = properties.FirstOrDefault(kvp => kvp.Key == nameof(ComplianceSchemaMetadata.metadataType));
                        var details = properties.FirstOrDefault(kvp => kvp.Key == nameof(ComplianceSchemaMetadata.details));
                        metadata.Add(new ComplianceSchemaMetadata(Guid.Parse(metadataType.Value.ToString()!), details.Value.ToString()!));
                    }
                }

                schema.ExtensionData[ComplianceKey] = metadata;
            }

            if (schema.Properties != default)
            {
                foreach (var property in schema.Properties)
                {
                    property.Value.EnsureComplianceMetadata();
                }
            }
        }
    }

    /// <summary>
    /// Get compliance metadata from schema. This is not recursive.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetComplianceMetadata(this JsonSchema schema)
    {
        lock (schema)
        {
            if ((schema.ExtensionData?.ContainsKey(ComplianceKey) ?? false) &&
                schema.ExtensionData[ComplianceKey] is IEnumerable<ComplianceSchemaMetadata> allMetadata)
            {
                return allMetadata;
            }

            return [];
        }
    }

    /// <summary>
    /// Check recursively if the schema has compliance metadata.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasComplianceMetadata(this JsonSchema schema)
    {
        var hasMetadata = schema.ExtensionData?.ContainsKey(ComplianceKey) ?? false;

        if (!hasMetadata && schema.Properties != default)
        {
            foreach (var property in schema.GetFlattenedProperties())
            {
                hasMetadata = property.HasComplianceMetadata();
                if (hasMetadata) break;
            }
        }

        return hasMetadata;
    }
}
