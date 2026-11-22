// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="JsonObject"/> and its <see cref="JsonObjectSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class JsonObjectSurrogateConverter : IConverter<JsonObject, JsonObjectSurrogate>
{
    /// <inheritdoc/>
    public JsonObject ConvertFromSurrogate(in JsonObjectSurrogate surrogate) => (JsonObject)JsonNode.Parse(surrogate.Json)!;

    /// <inheritdoc/>
    public JsonObjectSurrogate ConvertToSurrogate(in JsonObject value) => new(value.ToJsonString());
}
