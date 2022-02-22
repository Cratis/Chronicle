// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/>.
/// </summary>
public static class JsonSchemaExtensions
{
    /// <summary>
    /// Ensure the metadata is correct with correct types.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure.</param>
    public static void EnsureCorrectMetadata(this JsonSchema schema)
    {
        if ((schema.ExtensionData?.ContainsKey(JsonSchemaGenerator.ComplianceKey) ?? false) &&
            schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] is object[] complianceObjects)
        {
            var metadata = new List<ComplianceSchemaMetadata>();
            foreach (IDictionary<string, object> properties in complianceObjects)
            {
                var metadataType = properties.FirstOrDefault(kvp => kvp.Key == nameof(ComplianceSchemaMetadata.metadataType));
                var details = properties.FirstOrDefault(kvp => kvp.Key == nameof(ComplianceSchemaMetadata.details));
                metadata.Add(new ComplianceSchemaMetadata(Guid.Parse(metadataType.Value.ToString()!), details.Value.ToString()!));
            }

            schema.ExtensionData[JsonSchemaGenerator.ComplianceKey] = metadata;
        }

        if (schema.Properties != default)
        {
            foreach (var property in schema.Properties)
            {
                property.Value.EnsureCorrectMetadata();
            }
        }
    }
}
