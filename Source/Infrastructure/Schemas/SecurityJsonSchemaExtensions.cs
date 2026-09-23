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
    /// The security metadata type written for a subject-scoped <c language="csharp">[Encrypted]</c> value - the
    /// one <see cref="HasSubjectIndependentSecurityMetadata(JsonSchema)"/> excludes. A raw string rather than the
    /// client's or kernel's own <c language="csharp">SecurityMetadataType</c> concept, because this project is
    /// shared by both and neither one's types are referenced from here - schema metadata is written and read as
    /// the wire strings both sides agree on.
    /// </summary>
    const string EncryptedSubjectMetadataType = "EncryptedSubject";

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

    /// <summary>
    /// Check recursively whether the schema carries security metadata whose key does not depend on a subject at
    /// all - a namespace- or global-scoped <c language="csharp">[Encrypted]</c> value.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    /// <remarks>
    /// A subject-scoped <c language="csharp">[Encrypted]</c> value is keyed the same way <c language="csharp">[PII]</c>
    /// is - by a resolved subject - so, like PII, there is nothing to release when no subject resolves. A
    /// namespace- or global-scoped value is keyed independently of any subject, so it still needs releasing even
    /// then. This is false for a schema that carries only subject-scoped security metadata (or none at all),
    /// unlike <see cref="HasSecurityMetadata(JsonSchema)"/>, which is true for any of the three.
    /// </remarks>
    public static bool HasSubjectIndependentSecurityMetadata(this JsonSchema schema) =>
        schema.HasSchemaMetadata(SchemaMetadataCategory.Security, metadataType => metadataType != EncryptedSubjectMetadataType);
}
