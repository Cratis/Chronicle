// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents a <see cref="JsonConverter"/> for <see cref="EventRedacted"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventRedactedConverters"/> class.
/// </remarks>
/// <param name="eventTypes"><see creF="IEventTypes"/> for event type resolution.</param>
internal class EventRedactedConverters(IEventTypes eventTypes) : JsonConverter<EventRedacted>
{
    /// <inheritdoc/>
    public override EventRedacted? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var content = JsonSerializer.Deserialize<RedactionEventContent>(ref reader, options)!;
        var eventType = eventTypes.GetClrTypeFor(content.OriginalEventType);

        return new EventRedacted(
            content.Reason,
            eventType,
            content.Occurred,
            content.CorrelationId,
            content.Causation,
            content.CausedBy);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EventRedacted value, JsonSerializerOptions options)
    {
        var content = new RedactionEventContent(
             value.Reason,
             eventTypes.GetEventTypeFor(value.OriginalEventType).Id,
             value.Occurred,
             value.CorrelationId,
             value.Causation,
             value.CausedBy);

        JsonSerializer.Serialize(writer, content, options);
    }
}
