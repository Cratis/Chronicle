// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts the wire representation of event content (a JSON-encoded string) to the <see cref="JsonObject"/>
/// the internal event sequence API works with.
/// </summary>
internal static class JsonContentConverters
{
    /// <summary>
    /// Parses a JSON-encoded string into a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="content">The JSON-encoded string.</param>
    /// <returns>The parsed <see cref="JsonObject"/>.</returns>
    public static JsonObject ToJsonObject(this string content) => JsonNode.Parse(content)!.AsObject();
}
