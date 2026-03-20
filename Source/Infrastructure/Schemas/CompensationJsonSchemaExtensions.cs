// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/> for event compensation metadata.
/// </summary>
public static class CompensationJsonSchemaExtensions
{
    /// <summary>
    /// The key used in <see cref="JsonSchema.ExtensionData"/> to store the compensated event type identifier.
    /// </summary>
    public const string CompensationForKey = "compensationFor";

    /// <summary>
    /// Sets the compensation event type identifier in the schema's extension data.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to set on.</param>
    /// <param name="eventTypeId">The event type identifier of the event being compensated for.</param>
    public static void SetCompensationFor(this JsonSchema schema, string eventTypeId)
    {
        schema.ExtensionData ??= new Dictionary<string, object?>();
        schema.ExtensionData[CompensationForKey] = eventTypeId;
    }

    /// <summary>
    /// Gets the compensated event type identifier from the schema's extension data.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to get from.</param>
    /// <returns>The event type identifier, or <see langword="null"/> if not set.</returns>
    public static string? GetCompensationFor(this JsonSchema schema)
    {
        if (schema.ExtensionData?.TryGetValue(CompensationForKey, out var value) == true &&
            value is string eventTypeId)
        {
            return eventTypeId;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the schema has compensation metadata set.
    /// </summary>
    /// <param name="schema"><see cref="JsonSchema"/> to check.</param>
    /// <returns><see langword="true"/> if the schema has a compensation event type set; otherwise <see langword="false"/>.</returns>
    public static bool IsCompensation(this JsonSchema schema) =>
        schema.ExtensionData?.ContainsKey(CompensationForKey) == true;
}
