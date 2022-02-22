// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using NJsonSchema;

namespace Aksio.Cratis.Events.Schemas;

/// <summary>
/// Extension methods for working with <see cref="JsonSchema"/> and specific metadata.
/// </summary>
public static class SchemaExtensionMethods
{
    const string DisplayNameExtension = "displayName";
    const string GenerationExtension = "generation";

    /// <summary>
    /// Set the display name for a schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to use.</param>
    /// <param name="name">Name to set.</param>
    public static void SetDisplayName(this JsonSchema schema, string name)
    {
        schema.EnsureExtensionData();

        schema.ExtensionData[DisplayNameExtension] = name;
    }

    /// <summary>
    /// Set the generation for a schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to use.</param>
    /// <param name="generation">Generation to set.</param>
    public static void SetGeneration(this JsonSchema schema, uint generation)
    {
        schema.EnsureExtensionData();

        schema.ExtensionData[GenerationExtension] = generation;
    }

    /// <summary>
    /// Get the display name for a schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to use.</param>
    /// <returns>Name.</returns>
    public static string GetDisplayName(this JsonSchema schema)
    {
        return schema.ExtensionData?[DisplayNameExtension]?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Get the generation for a schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to use.</param>
    /// <returns>Generation.</returns>
    public static uint GetGeneration(this JsonSchema schema)
    {
        return uint.Parse(schema.ExtensionData?[GenerationExtension]?.ToString() ?? "1", CultureInfo.InvariantCulture);
    }

    static void EnsureExtensionData(this JsonSchema schema)
    {
        if (schema.ExtensionData == null) schema.ExtensionData = new Dictionary<string, object>();
    }
}
