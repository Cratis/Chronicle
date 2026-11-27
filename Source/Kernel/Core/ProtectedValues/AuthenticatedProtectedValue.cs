// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>Retains authenticated kind provenance outside the JSON document.</summary>
internal static class AuthenticatedProtectedValue
{
    static readonly ConditionalWeakTable<JsonNode, object> _typed = new();

    /// <summary>Checks whether a node was decoded using an authenticated v4 kind.</summary>
    /// <param name="node">The released node.</param>
    /// <returns>Whether schema-based kind restoration must be bypassed.</returns>
    internal static bool HasKind(JsonNode node) => _typed.TryGetValue(node, out _);

    /// <summary>Retains kind provenance on a value whose v4 envelope was authenticated.</summary>
    /// <param name="node">The decoded value or verified erased ciphertext.</param>
    /// <returns>The same node, without any JSON-visible marker.</returns>
    internal static JsonNode WithKind(JsonNode node)
    {
        _typed.GetValue(node, _ => new object());
        return node;
    }

    /// <summary>Decodes plaintext only after the envelope has been authenticated by decryption.</summary>
    /// <param name="plaintext">The authenticated plaintext.</param>
    /// <param name="kind">The authenticated kind, or null for legacy text.</param>
    /// <returns>The decoded value with out-of-band kind provenance.</returns>
    /// <exception cref="CryptographicException">The authenticated content does not match its kind.</exception>
    internal static JsonNode Decode(string plaintext, ProtectedValueKind? kind)
    {
        if (kind is null) return JsonValue.Create(plaintext);
        try
        {
            var node = kind == ProtectedValueKind.JsonString ? JsonValue.Create(plaintext) : JsonNode.Parse(plaintext);
            if (node is null || KindOf(node) != kind) throw new CryptographicException("Protected JSON kind does not match its content.");
            return WithKind(node);
        }
        catch (JsonException)
        {
            throw new CryptographicException("Protected JSON content is invalid.");
        }
    }

    /// <summary>Gets the wire kind from the value being protected, never from its schema.</summary>
    /// <param name="node">The original JSON value.</param>
    /// <returns>The stable envelope kind.</returns>
    /// <exception cref="ArgumentException">Null and undefined values have no protected kind.</exception>
    internal static ProtectedValueKind KindOf(JsonNode node) => node.GetValueKind() switch
    {
        JsonValueKind.String => ProtectedValueKind.JsonString,
        JsonValueKind.Number => ProtectedValueKind.JsonNumber,
        JsonValueKind.True or JsonValueKind.False => ProtectedValueKind.JsonBoolean,
        JsonValueKind.Array => ProtectedValueKind.JsonArray,
        JsonValueKind.Object => ProtectedValueKind.JsonObject,
        _ => throw new ArgumentException("A protected value must have a non-null JSON kind.", nameof(node))
    };
}
