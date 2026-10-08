// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Validates protected values for comparison without the erased-value fallbacks used by persistence release.
/// Traversal is shared with apply and release in <see cref="JsonSchemaMetadataManager"/>.
/// </summary>
internal static class StrictJsonSchemaRelease
{
    /// <summary>
    /// Attempts to recover the complete original value, including its JSON kind.
    /// </summary>
    /// <param name="schema">The protected boundary's schema.</param>
    /// <param name="handler">The metadata handler.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="identifier">The protection identifier.</param>
    /// <param name="node">The stored value.</param>
    /// <returns>The original value, or null if it cannot be recovered without loss.</returns>
    internal static async Task<JsonNode?> ReleaseValue(JsonSchema schema, IJsonSchemaMetadataValueHandler handler, EventStoreName eventStore, EventStoreNamespaceName @namespace, string identifier, JsonNode node)
    {
        var released = await handler.TryRelease(eventStore, @namespace, identifier, node);
        var type = ProtectedType(schema);
        if (type != JsonObjectType.String && released is JsonValue scalar && scalar.TryGetValue<string>(out var text))
        {
            released = JsonNode.Parse(text);
        }

        var valid = type switch
        {
            JsonObjectType.String => released?.GetValueKind() == JsonValueKind.String,
            JsonObjectType.Boolean => released?.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
            JsonObjectType.Integer or JsonObjectType.Number => released?.GetValueKind() == JsonValueKind.Number,
            JsonObjectType.Array => released is JsonArray,
            JsonObjectType.Object => released is JsonObject,
            _ => false
        };
        return valid ? released : null;
    }

    /// <summary>
    /// Determines whether a protected value has one unambiguous non-null JSON kind.
    /// </summary>
    /// <param name="schema">The protected boundary's schema.</param>
    /// <returns>Whether the codec's untyped text can be restored without ambiguity.</returns>
    internal static bool HasSupportedType(JsonSchema schema) =>
        ProtectedType(schema) is JsonObjectType.String or JsonObjectType.Integer or JsonObjectType.Number or JsonObjectType.Boolean or JsonObjectType.Array or JsonObjectType.Object;

    static JsonObjectType ProtectedType(JsonSchema schema)
    {
        // Do not use ActualTypeSchema here: for a union it selects the first non-null branch,
        // which would hide exactly the ambiguity this operation must reject.
        var type = schema.Type;
        if (schema.HasReference)
        {
            type |= ProtectedType(schema.Reference!);
        }

        foreach (var alternative in schema.OneOf.Concat(schema.AnyOf).Concat(schema.AllOf))
        {
            var alternativeType = ProtectedType(alternative);
            if (alternativeType == JsonObjectType.None && alternative.Type != JsonObjectType.Null)
            {
                return JsonObjectType.None;
            }

            type |= alternativeType;
        }

        return type & ~JsonObjectType.Null;
    }
}
