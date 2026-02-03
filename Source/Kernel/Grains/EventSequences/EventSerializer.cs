// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventTypes;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSerializer"/>.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for working with event types.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between expando objects to and from json.</param>
/// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use.</param>
public class EventSerializer(
    IEventTypes eventTypes,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions serializerOptions) : IEventSerializer
{
    readonly JsonSerializerOptions _serializerOptions = serializerOptions;

    /// <inheritdoc/>
    public object Deserialize(Type type, JsonObject json) =>
        json.Deserialize(type, _serializerOptions)!;

    /// <inheritdoc/>
    public object Deserialize(AppendedEvent @event)
    {
        var type = eventTypes.GetClrTypeFor(@event.Context.EventType.Id);
        var schema = eventTypes.GetJsonSchema(type);
        var contentAsJson = expandoObjectConverter.ToJsonObject(@event.Content, schema);
        return contentAsJson.Deserialize(type, _serializerOptions)!;
    }

    /// <inheritdoc/>
    public JsonObject Serialize(object @event) =>
        (JsonSerializer.SerializeToNode(@event, _serializerOptions) as JsonObject)!;
}
