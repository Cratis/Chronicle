// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/> for compliance.
/// </summary>
/// <remarks>
/// This is the <see cref="SchemaMetadataCategory.Compliance"/> facade over the generalized
/// <see cref="SchemaMetadataExtensions"/> mechanics, kept as its own named surface — with its own key, entirely
/// unchanged from before schema metadata was generalized — because compliance metadata is read by name in
/// existing stored schemas. See <see cref="SecurityJsonSchemaExtensions"/> for the equivalent, entirely separate
/// surface for security metadata, and <see cref="SchemaMetadataCategory"/> for why the two are never allowed
/// to mix.
/// </remarks>
public static class ComplianceJsonSchemaExtensions
{
    /// <summary>
    /// The key of the compliance extension data.
    /// </summary>
    public const string ComplianceKey = "compliance";

    /// <summary>
    /// Ensure the compliance metadata on the schema node itself is typed rather than raw JSON.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to ensure.</param>
    /// <remarks>
    /// This normalizes only the node it is given, and deliberately so. Compliance metadata can sit at any depth
    /// — inside a value object, inside an array's item schema — but every reader goes through
    /// <see cref="GetComplianceMetadata(JsonSchema)"/>, which accepts the raw <see cref="System.Text.Json.Nodes.JsonArray"/> form just
    /// as readily as the typed one, for whichever node it is handed. Walking the whole schema here would deep
    /// clone every nested node on each stored-schema load and change nothing about what those readers see.
    /// </remarks>
    public static void EnsureComplianceMetadata(this JsonSchema schema) => schema.EnsureSchemaMetadata(SchemaMetadataCategory.Compliance);

    /// <summary>
    /// Ensure the compliance metadata is correct with correct types.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to ensure.</param>
    public static void EnsureComplianceMetadata(this JsonSchemaProperty property) => property.EnsureSchemaMetadata(SchemaMetadataCategory.Compliance);

    /// <summary>
    /// Get compliance metadata from schema. This is not recursive.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetComplianceMetadata(this JsonSchema schema) => schema.GetSchemaMetadata(SchemaMetadataCategory.Compliance);

    /// <summary>
    /// Get compliance metadata from property. This is not recursive.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to get from.</param>
    /// <returns>Collection of <see cref="ComplianceSchemaMetadata"/>.</returns>
    public static IEnumerable<ComplianceSchemaMetadata> GetComplianceMetadata(this JsonSchemaProperty property) => property.GetSchemaMetadata(SchemaMetadataCategory.Compliance);

    /// <summary>
    /// Check recursively if the schema has compliance metadata.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    /// <remarks>
    /// The recursive walk is run once per schema instance and its result memoized, because the answer is invoked for
    /// every appended and read event and a schema is effectively immutable once built.
    /// </remarks>
    public static bool HasComplianceMetadata(this JsonSchema schema) => schema.HasSchemaMetadata(SchemaMetadataCategory.Compliance);

    /// <summary>
    /// Check if the property has compliance metadata.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasComplianceMetadata(this JsonSchemaProperty property) => property.HasSchemaMetadata(SchemaMetadataCategory.Compliance);
}
