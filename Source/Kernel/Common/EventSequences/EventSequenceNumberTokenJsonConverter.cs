// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> that can convert to and from <see cref="EventSequenceNumberToken"/>.
/// </summary>
public class EventSequenceNumberTokenJsonConverter : JsonConverter<EventSequenceNumberToken>
{
    /// <inheritdoc/>
    public override EventSequenceNumberToken? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetUInt64());

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EventSequenceNumberToken value, JsonSerializerOptions options) =>
        writer.WriteNumberValue((ulong)value.SequenceNumber);
}
