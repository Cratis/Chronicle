// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Resolves the parts of a <see cref="JsonSchema"/> the <see cref="ExpandoObjectConverter"/> converts against.
/// </summary>
static class ConverterSchemas
{
    /// <summary>
    /// Gets the format of a schema, falling back to the format of its actual type schema.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/>.</param>
    /// <returns>The format, which can be null.</returns>
    public static string FormatOf(JsonSchema schema) => schema.Format ?? schema.ActualTypeSchema.Format!;

    /// <summary>
    /// Gets the schema describing the object a value is converted to, which for an array is the schema of its items.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The <see cref="JsonSchema"/> of the object.</returns>
    public static JsonSchema ObjectSchemaOf(JsonSchema schema) =>
        schema.IsArray ? schema.Item!.Reference ?? schema.Item : schema.ActualTypeSchema;

    /// <summary>
    /// Gets the actual schema of a value. Nullable values are represented as a discriminated union with a null value,
    /// as can other types be, which makes it important to get the actual property definition.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The <see cref="JsonSchema"/> of the non-null branch, or the schema itself when there is none.</returns>
    public static JsonSchema ResolveNullable(JsonSchema schema)
    {
        if (schema.OneOf.Count == 0)
        {
            return schema;
        }

        var oneOfSchema = schema.OneOf.FirstOrDefault(_ => _.Type != JsonObjectType.Null);
        return oneOfSchema ?? schema;
    }

    /// <summary>
    /// Gets the type of a value, without null, following a reference when the schema declares no type of its own.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value, already resolved for nullability.</param>
    /// <returns>The <see cref="JsonObjectType"/>.</returns>
    public static JsonObjectType TypeOf(JsonSchema schema)
    {
        var type = (schema.Type == JsonObjectType.None && schema.HasReference) ? schema.Reference!.Type : schema.Type;
        return type.HasFlag(JsonObjectType.Null) ? type ^ JsonObjectType.Null : type;
    }

    /// <summary>
    /// Gets the type of a value, without null, resolving a nullable union first.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The <see cref="JsonObjectType"/>.</returns>
    public static JsonObjectType EffectiveType(JsonSchema schema) => TypeOf(ResolveNullable(schema));

    /// <summary>
    /// Gets the enumeration schema of a value, when it is one.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>The enumeration <see cref="JsonSchema"/>, or null when the value is not an enumeration.</returns>
    public static JsonSchema? EnumerationOf(JsonSchema schema)
    {
        if (schema.Reference?.IsEnumeration == true)
        {
            return schema.Reference;
        }

        return schema.IsEnumeration ? schema : null;
    }

    /// <summary>
    /// Gets whether the schema marks its value as protected. Asked for every string value converted.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> of the value.</param>
    /// <returns>True when the value is protected.</returns>
    public static bool HasProtectedValueMetadata(JsonSchema schema)
    {
        var reach = schema.MetadataReferences;
        return reach.Indeterminate || reach.Schemas.Exists(HasOwnProtectedMetadata);
    }

    static bool HasOwnProtectedMetadata(JsonSchema schema) =>
        (schema.HasComplianceMetadata() && schema.GetComplianceMetadata().Any()) ||
        (schema.HasSecurityMetadata() && schema.GetSecurityMetadata().Any());
}
