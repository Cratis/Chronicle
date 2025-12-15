// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Defines a serializer of events.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serialize an event to JSON.
    /// </summary>
    /// <param name="event">The event instance to serialize.</param>
    /// <returns>Serialized JSON.</returns>
    Task<JsonObject> Serialize(object @event);

    /// <summary>
    /// Deserialize a JSON representation of an event to a specific type.
    /// </summary>
    /// <param name="type">Type to deserialize to.</param>
    /// <param name="json">JSON to deserialize.</param>
    /// <returns>Deserialized instance.</returns>
    Task<object> Deserialize(Type type, JsonObject json);

    /// <summary>
    /// Get the deserialized content from an <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to get content from.</param>
    /// <returns>The deserialized event content.</returns>
    Task<object> Deserialize(AppendedEvent @event);
}
