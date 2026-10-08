// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Creates and detects the opaque markers that stand in for protected values during content verification.
/// </summary>
internal static class VerificationMarkers
{
    /// <summary>
    /// The text every verification marker starts with.
    /// </summary>
    internal const string Prefix = "chronicle-verification:";

    /// <summary>
    /// Creates a marker prefix carrying a random nonce, so no migration or stored content can name a marker in advance.
    /// </summary>
    /// <returns>The prefix to append a marker index to.</returns>
    internal static string NewPrefix() => $"{Prefix}{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}:";

    /// <summary>
    /// Checks whether any property name or string value contains the marker text.
    /// </summary>
    /// <param name="node">The node to inspect.</param>
    /// <returns>True when content cannot be told apart from a marker.</returns>
    internal static bool AppearIn(JsonNode? node) => node switch
    {
        JsonObject value => value.Any(_ => _.Key.Contains(Prefix, StringComparison.Ordinal) || AppearIn(_.Value)),
        JsonArray value => value.Any(AppearIn),
        JsonValue value => value.GetValueKind() == JsonValueKind.String && value.GetValue<string>().Contains(Prefix, StringComparison.Ordinal),
        _ => false
    };
}
