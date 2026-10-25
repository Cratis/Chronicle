// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Reads the kernel's observation flags as names or legacy numbers, preserving their values.
/// </summary>
internal sealed class EventObservationStateJsonConverter : JsonConverter<EventObservationState>
{
    /// <inheritdoc/>
    public override EventObservationState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var result = EventObservationState.None;
            foreach (var name in reader.GetString()!.Split(','))
            {
                result |= name.Trim().ToUpperInvariant() switch
                {
                    "NONE" => EventObservationState.None,
                    "INITIAL" => EventObservationState.Initial,
                    "REPLAY" => EventObservationState.Replay,
                    _ => throw new JsonException("Invalid event observation state.")
                };
            }

            return result;
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value) &&
            (value & ~(int)(EventObservationState.Initial | EventObservationState.Replay)) == 0)
        {
            return (EventObservationState)value;
        }

        throw new JsonException("Invalid event observation state.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EventObservationState value, JsonSerializerOptions options) => writer.WriteNumberValue((int)value);
}
