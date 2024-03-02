// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Aksio.Cratis.Events;

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
    /// Deserialize a JSON representation of an event to a specific type.
    /// </summary>
    /// <param name="type">Type to deserialize to.</param>
    /// <param name="expandoObject">Object to deserialize.</param>
    /// <returns>Deserialized instance.</returns>
    Task<object> Deserialize(Type type, ExpandoObject expandoObject);

    /// <summary>
    /// Deserialize an <see cref="AppendedEvent"/> to its actual type.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to deserialize.</param>
    /// <returns>The deserialized event in the target CLR type.</returns>
    Task<object> Deserialize(AppendedEvent @event);
}
