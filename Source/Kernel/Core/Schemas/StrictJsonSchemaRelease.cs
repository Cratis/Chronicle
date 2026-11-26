// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Validates protected values for comparison without the erased-value fallbacks used by persistence release.
/// Traversal is shared with apply and release in <see cref="JsonSchemaMetadataManager"/>.
/// </summary>
internal sealed class StrictJsonSchemaRelease
{
    readonly LegacyProtectedTypes _types = new();

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
    internal async Task<JsonNode?> ReleaseValue(JsonSchema schema, IJsonSchemaMetadataValueHandler handler, EventStoreName eventStore, EventStoreNamespaceName @namespace, string identifier, JsonNode node)
    {
        var released = await handler.TryRelease(eventStore, @namespace, identifier, node);
        return released is null ? null : TryRestore(schema, released);
    }

    /// <summary>
    /// Attempts to restore the original JSON kind of an already released value.
    /// </summary>
    /// <param name="schema">The protected boundary's schema.</param>
    /// <param name="released">The released value, which is text for non-string protected types.</param>
    /// <returns>The original value, or null if it cannot be restored without loss.</returns>
    internal JsonNode? TryRestore(JsonSchema schema, JsonNode released)
    {
        if (AuthenticatedProtectedValue.HasKind(released)) return released;
        var type = _types.For(schema);
        released = RestoreJsonKind(type, released)!;
        return HasJsonKind(type, released) ? released : null;
    }

    /// <summary>
    /// Determines whether a protected value has one unambiguous non-null JSON kind.
    /// </summary>
    /// <param name="schema">The protected boundary's schema.</param>
    /// <returns>Whether the codec's untyped text can be restored without ambiguity.</returns>
    internal bool HasSupportedType(JsonSchema schema) => _types.IsSupported(schema);

    static JsonNode? RestoreJsonKind(JsonObjectType type, JsonNode released)
    {
        if (type == JsonObjectType.String || released is not JsonValue scalar || !scalar.TryGetValue<string>(out var text))
        {
            return released;
        }

        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static bool HasJsonKind(JsonObjectType type, JsonNode? released) => type switch
    {
        JsonObjectType.String => released?.GetValueKind() == JsonValueKind.String,
        JsonObjectType.Boolean => released?.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
        JsonObjectType.Integer or JsonObjectType.Number => released?.GetValueKind() == JsonValueKind.Number,
        JsonObjectType.Array => released is JsonArray,
        JsonObjectType.Object => released is JsonObject,
        _ => false
    };
}
