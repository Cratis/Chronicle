// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> for <see cref="EventSourceId{T}"/> that serializes
/// the typed value as a plain string for wire compatibility with <see cref="EventSourceId"/>.
/// </summary>
/// <typeparam name="T">The underlying type wrapped by <see cref="EventSourceId{T}"/>.</typeparam>
public class EventSourceIdJsonConverter<T> : JsonConverter<EventSourceId<T>>
    where T : IComparable
{
    /// <inheritdoc/>
    public override EventSourceId<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? null : (EventSourceId<T>)value;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EventSourceId<T> value, JsonSerializerOptions options)
    {
        EventSourceId id = value;
        writer.WriteStringValue(id.Value);
    }
}
