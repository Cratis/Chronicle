// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> that reads and writes an <see cref="ImmutableJsonObject"/> as the JSON object it holds.
/// </summary>
public sealed class ImmutableJsonObjectJsonConverter : JsonConverter<ImmutableJsonObject>
{
    /// <inheritdoc/>
    public override ImmutableJsonObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : ImmutableJsonObject.From(JsonNode.Parse(ref reader)?.AsObject());

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ImmutableJsonObject value, JsonSerializerOptions options) =>
        writer.WriteRawValue(value.Json);
}
