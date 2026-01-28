// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/> for dynamic types.
/// </summary>
public static class DynamicTypeSchemaExtensions
{
    /// <summary>
    /// The key for the dynamic type metadata in extension data.
    /// </summary>
    public const string DynamicTypeKey = "dynamic";

    /// <summary>
    /// Mark a schema property as dynamic (dictionary with runtime-determined keys).
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to mark.</param>
    public static void MarkAsDynamic(this JsonSchema schema)
    {
        schema.ExtensionData ??= new Dictionary<string, object?>();
        schema.ExtensionData[DynamicTypeKey] = true;
        schema.Format = TypeFormats.DynamicFormat;
    }

    /// <summary>
    /// Check if a schema property is marked as dynamic.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns>True if dynamic, false otherwise.</returns>
    public static bool IsDynamic(this JsonSchema schema)
    {
        if (schema.ExtensionData?.ContainsKey(DynamicTypeKey) ?? false)
        {
            return schema.ExtensionData[DynamicTypeKey] is true;
        }

        return schema.Format == TypeFormats.DynamicFormat;
    }
}
