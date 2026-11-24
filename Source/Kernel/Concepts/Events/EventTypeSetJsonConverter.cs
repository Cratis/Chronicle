// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the <see cref="JsonConverter{T}"/> for <see cref="EventTypeSet"/>, which reads a list of event types back into the shared set.
/// </summary>
public sealed class EventTypeSetJsonConverter : JsonConverter<EventTypeSet>
{
    /// <inheritdoc/>
    public override EventTypeSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null
            ? null
            : EventTypeSet.From(JsonSerializer.Deserialize<EventType[]>(ref reader, options));

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EventTypeSet value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.ToArray(), options);
}
