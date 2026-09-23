// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/> for security - the <see cref="SchemaMetadataCategory.Security"/>
/// counterpart to <see cref="ComplianceJsonSchemaExtensions"/>, built on the same generalized
/// <see cref="SchemaMetadataExtensions"/> mechanics.
/// </summary>
/// <remarks>
/// Security metadata (currently: <c language="csharp">[Encrypted]</c>) is stored under its own key, entirely
/// separate from <see cref="ComplianceJsonSchemaExtensions.ComplianceKey"/> - see
/// <see cref="SchemaMetadataCategory"/> for why the two are never allowed to mix.
/// </remarks>
public static class SecurityJsonSchemaExtensions
{
    /// <summary>
    /// The key of the security extension data.
    /// </summary>
    public const string SecurityKey = "security";

    /// <summary>
    /// Ensure the security metadata on the schema node itself is typed rather than raw JSON.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure.</param>
    public static void EnsureSecurityMetadata(this JsonSchema schema) => schema.EnsureSchemaMetadata(SchemaMetadataCategory.Security);

    /// <summary>
    /// Get security metadata from schema. This is not recursive.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetSecurityMetadata(this JsonSchema schema) => schema.GetSchemaMetadata(SchemaMetadataCategory.Security);

    /// <summary>
    /// Get security metadata from property. This is not recursive.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to get from.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetSecurityMetadata(this JsonSchemaProperty property) => property.GetSchemaMetadata(SchemaMetadataCategory.Security);

    /// <summary>
    /// Check recursively if the schema has security metadata.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasSecurityMetadata(this JsonSchema schema) => schema.HasSchemaMetadata(SchemaMetadataCategory.Security);

    /// <summary>
    /// Check if the property has security metadata.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasSecurityMetadata(this JsonSchemaProperty property) => property.HasSchemaMetadata(SchemaMetadataCategory.Security);
}
