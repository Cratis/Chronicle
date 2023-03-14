// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents a <see cref="JsonConverter"/> for <see cref="EventRedacted"/>.
/// </summary>
public class EventRedactedConverter : JsonConverter<EventRedacted>
{
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRedactedConverter"/> class.
    /// </summary>
    /// <param name="eventTypes"><see creF="IEventTypes"/> for event type resolution.</param>
    public EventRedactedConverter(IEventTypes eventTypes)
    {
        _eventTypes = eventTypes;
    }

    /// <inheritdoc/>
    public override EventRedacted? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var content = JsonSerializer.Deserialize<RedactionEventContent>(ref reader, options)!;
        var eventType = _eventTypes.GetClrTypeFor(content.OriginalEventType);

        return new EventRedacted(
            content.Reason,
            eventType,
            content.Occurred,
            content.CausationId,
            content.CorrelationId,
            content.CausedBy);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EventRedacted value, JsonSerializerOptions options)
    {
        var content = new RedactionEventContent(
             value.Reason,
             _eventTypes.GetEventTypeFor(value.OriginalEventType).Id,
             value.Occurred,
             value.CausationId,
             value.CorrelationId,
             value.CausedBy);

        JsonSerializer.Serialize(writer, content, options);
    }
}
