// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Events.Schemas;

/// <summary>
/// Extension methods for working with <see cref="JsonSchema"/> and specific metadata.
/// </summary>
public static class SchemaExtensionMethods
{
    const string EventTypeExtension = "eventType";
    const string IsPublicExtension = "isPublic";
    const string DisplayNameExtension = "displayName";
    const string GenerationExtension = "generation";

    /// <summary>
    /// Set the event type extension in the schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to use.</param>
    /// <param name="eventType">EventType to set.</param>
    public static void SetEventType(this JsonSchema schema, EventType eventType)
    {
        schema.EnsureExtensionData();
        schema.ExtensionData[EventTypeExtension] = $"{eventType.Id}+{eventType.Generation}";
    }

    /// <summary>
    /// Get the event type extension from the schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>True if event type is set, false if not.</returns>
    public static bool HasEventType(this JsonSchema schema) => schema.ExtensionData?[EventTypeExtension] is not null;

    /// <summary>
    /// Get the event type extension from the schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>The event type. If there is no event type in the schema, it will return an unknown event type.</returns>
    public static EventType GetEventType(this JsonSchema schema)
    {
        if (schema.HasEventType())
        {
            var elements = schema.ExtensionData![EventTypeExtension]!.ToString()!.Split('+');
            return new(Guid.Parse(elements[0]), uint.Parse(elements[1]));
        }

        return new(EventTypeId.Unknown, EventGeneration.First);
    }

    /// <summary>
    /// Set the isPublic extension in the schema.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to use.</param>
    /// <param name="isPublic">State to set.</param>
    public static void SetIsPublic(this JsonSchema schema, bool isPublic)
    {
        schema.EnsureExtensionData();
        schema.ExtensionData[IsPublicExtension] = isPublic;
    }

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
        return uint.Parse(schema.ExtensionData?[GenerationExtension]?.ToString() ?? "1");
    }

    static void EnsureExtensionData(this JsonSchema schema)
    {
        if (schema.ExtensionData == null) schema.ExtensionData = new Dictionary<string, object>();
    }
}
